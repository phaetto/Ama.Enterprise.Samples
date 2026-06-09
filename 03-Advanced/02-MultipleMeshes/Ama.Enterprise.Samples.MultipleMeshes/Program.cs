namespace Ama.Enterprise.Samples.MultipleMeshes;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ama.Enterprise.CRDT.Distributed.Extensions;
using Ama.Enterprise.CRDT.Distributed.Services;
using Ama.Enterprise.FeatureFlags.Extensions;
using Ama.Enterprise.FeatureFlags.Services;
using Ama.Enterprise.Licensing.Extensions;
using Ama.Enterprise.P2p.Extensions;
using Ama.Enterprise.Samples.Common.Logging;
using Ama.Enterprise.Samples.Common.Network;
using Ama.Enterprise.Samples.Common.UI;

internal sealed class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args
        });

        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
        builder.Logging.AddFilter("System", LogLevel.Warning);
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new LockedConsoleLoggerProvider());

        // 1. Configure Open Source Licensing.
        builder.Services.ConfigureAmaCommunityLicense();

        var mode = args.Length > 0 ? args[0].Trim().ToLowerInvariant() : "alpha";
        if (mode is not ("alpha" or "beta" or "bridge"))
        {
            mode = "alpha";
        }

        var basePort = 8100;
        if (args.Length > 1 && int.TryParse(args[1], out var parsedPort))
        {
            basePort = parsedPort;
        }

        var primaryPort = NetworkUtilities.GetNextAvailablePort(basePort);
        var replicaId = $"node-{mode}-{primaryPort}";

        // 2. Register the CRDT replica with a unique ID and configure synchronization settings.
        builder.Services.AddDistributedCrdtReplica(replicaId);

        builder.Services.AddDistributedCrdtCore(options =>
        {
            options.ActiveSyncEnabled = true;
            options.CheckpointIntervalSeconds = (int)TimeSpan.FromHours(1).TotalSeconds;
            options.AntiEntropyIntervalSeconds = 5;
            options.AntiEntropyInitialDelaySeconds = 3;
        });

        // 3. Add the Feature Flags services and register the console state observer via DI.
        builder.Services.AddSingleton<CancellationTokenSource>();
        builder.Services.AddSingleton<InteractiveConsoleApp>();
        builder.Services.AddSingleton<IFeatureFlagStateObserver, ConsoleFeatureFlagObserver>();
        builder.Services.AddFeatureFlags();

        // 4. Configure and link one or more P2P meshes depending on the selected node topology mode.
        var nextPortCursor = primaryPort;

        if (mode is "alpha" or "bridge")
        {
            var alphaTcpPort = nextPortCursor;
            var alphaHandshakePort = NetworkUtilities.GetNextAvailablePort(alphaTcpPort + 100);
            nextPortCursor = NetworkUtilities.GetNextAvailablePort(alphaTcpPort + 1);

            ConfigureMesh(
                builder.Services,
                Constants.AlphaMeshName,
                replicaId,
                alphaTcpPort,
                alphaHandshakePort,
                Constants.AlphaMulticastPort);
        }

        if (mode is "beta" or "bridge")
        {
            var betaTcpPort = nextPortCursor;
            var betaHandshakePort = NetworkUtilities.GetNextAvailablePort(betaTcpPort + 100);

            ConfigureMesh(
                builder.Services,
                Constants.BetaMeshName,
                replicaId,
                betaTcpPort,
                betaHandshakePort,
                Constants.BetaMulticastPort);
        }

        using var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("MultipleMeshesSample");

        // Get the feature flag cluster manager for this node.
        var scopeManager = host.Services.GetRequiredService<DistributedCrdtScopeManager>();
        var crdtScope = scopeManager.GetOrCreateScope(replicaId);
        var clusterManager = crdtScope.ServiceProvider.GetRequiredService<IFeatureFlagCrdtManager>();

        var cts = host.Services.GetRequiredService<CancellationTokenSource>();
        var app = host.Services.GetRequiredService<InteractiveConsoleApp>();

        var meshDescription = mode switch
        {
            "alpha" => $"Zone A ({Constants.AlphaMeshName} on UDP {Constants.AlphaMulticastPort})",
            "beta" => $"Zone B ({Constants.BetaMeshName} on UDP {Constants.BetaMulticastPort})",
            _ => $"Bridge Gateway ({Constants.AlphaMeshName} [UDP {Constants.AlphaMulticastPort}] <-> {Constants.BetaMeshName} [UDP {Constants.BetaMulticastPort}])"
        };

        app.SetTitle($"Multiple Meshes Showcase [{mode.ToUpperInvariant()}]");
        app.SetCurrentPort(primaryPort);
        app.SetIntroText($"Replica: '{replicaId}' | Topology: {meshDescription}\nUsage: dotnet run -- <alpha|beta|bridge> [port]");

        // Define console commands to interact with feature flags.
        app.RegisterCommand("set", "Adds or Updates a flag (e.g., set MyFlag true)", async (parts, token) =>
        {
            if (parts.Length >= 3 && bool.TryParse(parts[2], out var isEnabled))
            {
                await clusterManager.SetFlagAsync(parts[1], isEnabled, cancellationToken: token).ConfigureAwait(false);
            }
            else
            {
                ConsoleHelper.WriteLineLocked("Usage: set <name> <true|false>");
                await Task.Delay(1500, token).ConfigureAwait(false);
            }
        });

        app.RegisterCommand("del", "Removes a flag entirely (e.g., del MyFlag)", async (parts, token) =>
        {
            if (parts.Length >= 2)
            {
                await clusterManager.RemoveFlagAsync(parts[1], token).ConfigureAwait(false);
            }
            else
            {
                ConsoleHelper.WriteLineLocked("Usage: del <name>");
                await Task.Delay(1500, token).ConfigureAwait(false);
            }
        });

        // Define how the current feature flag state is displayed in the console.
        app.SetStateRenderer(async () =>
        {
            var flags = await clusterManager.GetFlagsAsync().ConfigureAwait(false);
            Console.WriteLine($"\n--- [Cluster State ({mode.ToUpperInvariant()})] ---");
            if (flags.Count == 0)
            {
                Console.WriteLine(" (No flags currently defined)");
            }
            else
            {
                foreach (var flag in flags.OrderBy(f => f.Key))
                {
                    Console.WriteLine($" => [{flag.Key}]: {(flag.Value.IsEnabled ? "ENABLED" : "DISABLED")}");
                }
            }
            Console.WriteLine("------------------------------------\n> ");
        });

        // Start the host and all background services before entering the UI loop.
        await host.StartAsync(cts.Token).ConfigureAwait(false);

        logger.LogInformation("Started Multiple Meshes Node in '{Mode}' mode (Replica: {ReplicaId})...", mode, replicaId);

        // Run the interactive console application.
        await app.RunAsync().ConfigureAwait(false);

        logger.LogInformation("Shutting down network services gracefully...");

        await host.StopAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private static void ConfigureMesh(
        IServiceCollection services,
        string meshName,
        string replicaId,
        int tcpListenPort,
        int udpHandshakePort,
        int multicastPort)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(meshName);
        ArgumentException.ThrowIfNullOrWhiteSpace(replicaId);

        // Link the P2P mesh network to the CRDT replica.
        services.AddDistributedCrdtReplica(replicaId, meshName);

        // Configure the P2P mesh network with its own isolated ports and discovery multicast channel.
        services.AddP2pMesh(meshName)
            .AddGossipNetwork(options =>
            {
                options.GossipInterval = TimeSpan.FromMilliseconds(1500);
                options.Fanout = 3;
                options.DefaultTimeToLive = 3;
            })
            .AddTcpTransport(options =>
            {
                options.ListenPort = tcpListenPort;
                options.ListenHost = Constants.LoopbackHost;
            })
            .AddUdpPeerDiscovery(options =>
            {
                options.MulticastAddress = Constants.MulticastAddress;
                options.MulticastPort = multicastPort;
                options.DiscoveryInterval = TimeSpan.FromSeconds(1);
                options.DiscoveryTimeout = TimeSpan.FromSeconds(10);
            })
            .AddUdpPeerHandshake(options =>
            {
                options.ListenPort = udpHandshakePort;
            });
    }
}
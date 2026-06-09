namespace Ama.Enterprise.Samples.Monitoring;

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

        var currentPort = 8100;
        if (args.Length > 0 && int.TryParse(args[0], out var parsedPort))
        {
            currentPort = parsedPort;
        }
        else
        {
            currentPort = NetworkUtilities.GetNextAvailablePort(currentPort);
        }

        var handshakePort = NetworkUtilities.GetNextAvailablePort(currentPort + 100);
        var replicaId = $"node-{currentPort}";

        // 2. Register the CRDT replica with a unique ID and configure synchronization settings.
        builder.Services.AddDistributedCrdtCore(options =>
        {
            options.ActiveSyncEnabled = true;
            options.CheckpointIntervalSeconds = (int)TimeSpan.FromHours(1).TotalSeconds;
            options.AntiEntropyIntervalSeconds = 5;
            options.AntiEntropyInitialDelaySeconds = 3;
        });

        // 3. Link the P2P mesh network to the CRDT replica.
        builder.Services.AddDistributedCrdtReplica(replicaId, "feature-flags-internal-mesh");

        // 4. Add the Feature Flags services and register the console state observer via DI.
        builder.Services.AddSingleton<CancellationTokenSource>();
        builder.Services.AddSingleton<InteractiveConsoleApp>();
        builder.Services.AddSingleton<IFeatureFlagStateObserver, ConsoleFeatureFlagObserver>();
        builder.Services.AddFeatureFlags();

        // 5. Configure the Mesh Network to use TCP transport and UDP discovery.
        builder.Services.AddP2pMesh("feature-flags-internal-mesh")
                .AddGossipNetwork(options =>
                {
                    options.GossipInterval = TimeSpan.FromMilliseconds(1500);
                    options.Fanout = 3;
                    options.DefaultTimeToLive = 3;
                })
                .AddTcpTransport(options =>
                {
                    options.ListenPort = currentPort;
                    options.ListenHost = "127.0.0.1";
                })
                .AddUdpPeerDiscovery(options =>
                {
                    options.MulticastAddress = "239.255.0.1";
                    options.MulticastPort = 8035;
                    options.DiscoveryInterval = TimeSpan.FromSeconds(1);
                    options.DiscoveryTimeout = TimeSpan.FromSeconds(10);
                })
                .AddUdpPeerHandshake(options =>
                {
                    options.ListenPort = handshakePort;
                });

        using var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("TcpUdpSample");

        // Get the feature flag cluster manager for this node.
        var scopeManager = host.Services.GetRequiredService<DistributedCrdtScopeManager>();
        var crdtScope = scopeManager.GetOrCreateScope(replicaId);
        var clusterManager = crdtScope.ServiceProvider.GetRequiredService<IFeatureFlagCrdtManager>();

        var cts = host.Services.GetRequiredService<CancellationTokenSource>();
        var app = host.Services.GetRequiredService<InteractiveConsoleApp>();
        app.SetTitle("TCP/UDP Feature Flags Showcase");
        app.SetCurrentPort(currentPort);
        app.SetIntroText("This application demonstrates TCP and UDP communication concepts to sync feature flags across clustered nodes.");

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
                await Task.Delay(1500, token); // Brief pause to display usage error
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
                await Task.Delay(1500, token); // Brief pause to display usage error
            }
        });

        // Define how the current feature flag state is displayed in the console.
        app.SetStateRenderer(async () =>
        {
            var flags = await clusterManager.GetFlagsAsync();
            Console.WriteLine("\n--- [Cluster State Synchronized] ---");
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

        logger.LogInformation("Starting TCP/UDP Node on port {Port}...", currentPort);

        // Run the interactive console application.
        await app.RunAsync().ConfigureAwait(false);

        logger.LogInformation("Shutting down network services gracefully...");

        await host.StopAsync(CancellationToken.None).ConfigureAwait(false);
    }
}
namespace Ama.Enterprise.Samples.TcpUdpFeatureFlags;

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
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddFilter("Microsoft", LogLevel.Warning);
            builder.AddFilter("System", LogLevel.Warning);
            builder.ClearProviders();
            builder.AddProvider(new LockedConsoleLoggerProvider());
        });

        // 1. Configure Open Source Licensing.
        services.ConfigureAmaCommunityLicense();

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
        services.AddDistributedCrdtReplica(replicaId);
        
        services.AddDistributedCrdtCore(options =>
        {
            options.ActiveSyncEnabled = true;
            options.CheckpointIntervalSeconds = (int)TimeSpan.FromHours(1).TotalSeconds;
            options.AntiEntropyIntervalSeconds = 5;
            options.AntiEntropyInitialDelaySeconds = 3;
        });

        // 3. Link the P2P mesh network to the CRDT replica.
        services.AddDistributedCrdtP2p("feature-flags-internal-mesh", replicaId);

        // 4. Add the Feature Flags services.
        services.AddFeatureFlags();

        // 5. Configure the Mesh Network to use TCP transport and UDP discovery.
        services.AddP2pMesh("feature-flags-internal-mesh")
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

        await using var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("TcpUdpSample");

        // Get the feature flag cluster manager for this node.
        var scopeManager = provider.GetRequiredService<DistributedCrdtScopeManager>();
        var crdtScope = scopeManager.GetOrCreateScope(replicaId);
        var clusterManager = crdtScope.ServiceProvider.GetRequiredService<IFeatureFlagClusterManager>();

        using var cts = new CancellationTokenSource();

        var app = new InteractiveConsoleApp(logger, "TCP/UDP Feature Flags Showcase", cts);
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

        // Redraw the console UI when the state changes.
        clusterManager.StateChanged += (sender, eventArgs) =>
        {
            app.RequestRedraw();
        };

        // Define how the current feature flag state is displayed in the console.
        app.SetStateRenderer(() =>
        {
            var flags = clusterManager.GetFlags();
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

        // Start all background services manually before entering the UI loop.
        var hostedServices = provider.GetServices<IHostedService>().ToList();
        foreach (var service in hostedServices)
        {
            await service.StartAsync(cts.Token).ConfigureAwait(false);
        }

        logger.LogInformation("Starting TCP/UDP Node on port {Port}...", currentPort);

        // Run the interactive console application.
        await app.RunAsync().ConfigureAwait(false);

        logger.LogInformation("Shutting down network services gracefully...");

        foreach (var service in hostedServices)
        {
            await service.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }
}
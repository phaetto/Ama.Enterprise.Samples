namespace Ama.Enterprise.Samples.Persistence;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ama.Enterprise.CRDT.Distributed.Extensions;
using Ama.Enterprise.CRDT.Distributed.Models;
using Ama.Enterprise.CRDT.Distributed.Services;
using Ama.Enterprise.FeatureFlags.Extensions;
using Ama.Enterprise.FeatureFlags.Services;
using Ama.Enterprise.Licensing.Extensions;
using Ama.Enterprise.P2p.Extensions;
using Ama.Enterprise.Samples.Common.Logging;
using Ama.Enterprise.Samples.Common.Network;
using Ama.Enterprise.Samples.Common.UI;
using Ama.Enterprise.Samples.Persistence.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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

        var currentPort = Constants.DefaultListenPort;
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

        // 2. Register the CRDT replica with a unique ID and configure synchronization & checkpointing settings.
        builder.Services.AddDistributedCrdtCore(options =>
        {
            options.ActiveSyncEnabled = true;
            options.CheckpointIntervalSeconds = 5;
            options.AntiEntropyIntervalSeconds = 5;
            options.AntiEntropyInitialDelaySeconds = 2;
            options.JournalMode = CrdtJournalMode.InMemoryBuffer;
            options.WalChannelCapacity = 10000;
            options.WalMaxBatchSize = 1000;
            options.WalDirectoryPath = AppContext.BaseDirectory;
        });

        // 3. Register our custom SQLite storage provider (IDistributedCrdtStorage) to persist documents, DVVs, and journals.
        builder.Services.AddDistributedCrdtStorage<SqliteCrdtStorage>();

        // 4. Link the P2P mesh network to the CRDT replica.
        builder.Services.AddDistributedCrdtReplica(replicaId, Constants.MeshName);

        // 5. Add the Feature Flags services and register the console state observer via DI.
        builder.Services.AddSingleton<CancellationTokenSource>();
        builder.Services.AddSingleton<InteractiveConsoleApp>();
        builder.Services.AddSingleton<IFeatureFlagStateObserver, ConsoleFeatureFlagObserver>();
        builder.Services.AddFeatureFlags();

        // 6. Configure the Mesh Network to use TCP transport and UDP discovery.
        builder.Services.AddP2pMesh(Constants.MeshName)
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
                    options.MulticastAddress = Constants.DefaultMulticastAddress;
                    options.MulticastPort = Constants.DefaultMulticastPort;
                    options.DiscoveryInterval = TimeSpan.FromSeconds(1);
                    options.DiscoveryTimeout = TimeSpan.FromSeconds(10);
                })
                .AddUdpPeerHandshake(options =>
                {
                    options.ListenPort = handshakePort;
                });

        using var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("PersistenceSample");

        // Resolve the feature flag cluster manager and custom storage for this node.
        var scopeManager = host.Services.GetRequiredService<DistributedCrdtScopeManager>();
        var crdtScope = scopeManager.GetOrCreateScope(replicaId);
        var clusterManager = crdtScope.ServiceProvider.GetRequiredService<IFeatureFlagCrdtManager>();
        var storage = crdtScope.ServiceProvider.GetRequiredService<IDistributedCrdtStorage>();

        var cts = host.Services.GetRequiredService<CancellationTokenSource>();
        var app = host.Services.GetRequiredService<InteractiveConsoleApp>();
        app.SetTitle($"SQLite Persistence CRDT Showcase ({replicaId})");
        app.SetCurrentPort(currentPort);
        app.SetIntroText(
            $"This application demonstrates custom SQLite persistence via IDistributedCrdtStorage.\n" +
            $"State, DVVs, and the operation journal are persisted to 'crdt_storage_{replicaId}.db'.\n" +
            $"Try setting flags, exiting, and restarting on port {currentPort} (dotnet run -- {currentPort}) to verify persistence!");

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

        // Define how the current feature flag state and SQLite storage metrics are displayed in the console.
        app.SetStateRenderer(async () =>
        {
            var flags = await clusterManager.GetFlagsAsync().ConfigureAwait(false);
            var dvvMap = await storage.LoadShardVersionVectorMapAsync(replicaId).ConfigureAwait(false);
            var totalPersistedSnapshots = dvvMap?.Keys.Count ?? 0;

            Console.WriteLine($"\n--- [Cluster State Synchronized | DB: crdt_storage_{replicaId}.db (Persisted Shards: {totalPersistedSnapshots})] ---");
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
            Console.WriteLine("------------------------------------------------------------------------------------------------\n> ");
        });

        // Start the host and all background services before entering the UI loop.
        await host.StartAsync(cts.Token).ConfigureAwait(false);

        logger.LogInformation("Starting SQLite Persistence Node '{ReplicaId}' on port {Port}...", replicaId, currentPort);

        // Run the interactive console application.
        await app.RunAsync().ConfigureAwait(false);

        logger.LogInformation("Shutting down network and flushing SQLite storage gracefully...");

        await host.StopAsync(CancellationToken.None).ConfigureAwait(false);
    }
}
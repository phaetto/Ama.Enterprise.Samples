namespace Ama.Enterprise.Samples.Sharding;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ama.Enterprise.CRDT.Distributed.Extensions;
using Ama.Enterprise.CRDT.Distributed.Services;
using Ama.Enterprise.Licensing.Extensions;
using Ama.Enterprise.P2p.Extensions;
using Ama.Enterprise.Samples.Common.Logging;
using Ama.Enterprise.Samples.Common.Network;
using Ama.Enterprise.Samples.Common.UI;
using Ama.Enterprise.Samples.Sharding.Extensions;
using Ama.Enterprise.Samples.Sharding.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal sealed class Program
{
    public static async Task Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

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

        var replicationFactor = Constants.DefaultReplicationFactor;
        if (args.Length > 1 && int.TryParse(args[1], out var parsedRf) && parsedRf >= 1)
        {
            replicationFactor = parsedRf;
        }

        var handshakePort = NetworkUtilities.GetNextAvailablePort(currentPort + 100);
        var replicaId = $"node-{currentPort}";

        // 2. Register the CRDT replica and enable Consistent Hash Ring Sharding.
        builder.Services.AddDistributedCrdtCore(options =>
        {
            options.EnableSharding = true;
            options.ShardReplicationFactor = replicationFactor;
            options.VirtualNodesPerReplica = 64;
            options.ActiveSyncEnabled = true;
            options.CheckpointIntervalSeconds = (int)TimeSpan.FromHours(1).TotalSeconds;
            options.AntiEntropyIntervalSeconds = 5;
            options.AntiEntropyInitialDelaySeconds = 2;
        });

        // 3. Register the custom IShardIdProvider, TenantFeatureFlagState document type, and observers.
        builder.Services.AddSingleton<CancellationTokenSource>();
        builder.Services.AddSingleton<InteractiveConsoleApp>();
        builder.Services.AddShardedTenantFeatureFlags();

        // 4. Link the P2P mesh network to the CRDT replica.
        builder.Services.AddDistributedCrdtReplica(replicaId, Constants.MeshName);

        // 5. Configure the Mesh Network to use TCP transport and UDP discovery.
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
                    options.MulticastAddress = Constants.MulticastAddress;
                    options.MulticastPort = Constants.MulticastPort;
                    options.DiscoveryInterval = TimeSpan.FromSeconds(1);
                    options.DiscoveryTimeout = TimeSpan.FromSeconds(10);
                })
                .AddUdpPeerHandshake(options =>
                {
                    options.ListenPort = handshakePort;
                });

        using var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("ShardingSample");

        var scopeManager = host.Services.GetRequiredService<DistributedCrdtScopeManager>();
        var crdtScope = scopeManager.GetOrCreateScope(replicaId);
        var tenantFlagManager = crdtScope.ServiceProvider.GetRequiredService<ITenantFlagManager>();

        var cts = host.Services.GetRequiredService<CancellationTokenSource>();
        var app = host.Services.GetRequiredService<InteractiveConsoleApp>();
        app.SetTitle($"Horizontal CRDT Sharding Showcase (Replica: {replicaId}, RF: {replicationFactor})");
        app.SetCurrentPort(currentPort);
        app.SetIntroText(
            "Demonstrates partitioning tenant CRDT documents across a consistent hash ring using IShardIdProvider.\n" +
            "Each node only holds active memory & Dotted Version Vectors (DVVs) for shards assigned to it,\n" +
            "while reads and writes to remote shards are transparently proxied via P2P RPC.");

        app.RegisterCommand("seed", "Creates 6 sample tenant documents (tenant-1 .. tenant-6) with initial flags", async (_, token) =>
        {
            for (var i = 1; i <= 6; i++)
            {
                var tenantId = $"tenant-{i}";
                await tenantFlagManager.SetFlagAsync(tenantId, "DarkMode", i % 2 == 0, token).ConfigureAwait(false);
                await tenantFlagManager.SetFlagAsync(tenantId, "BetaCheckout", i % 3 == 0, token).ConfigureAwait(false);
            }

            ConsoleHelper.WriteLineLocked("[Sharding] Seeded 6 sharded tenant documents across the hash ring.");
        });

        app.RegisterCommand("set", "Sets a flag on any tenant shard (e.g., set tenant-1 NewBilling true)", async (parts, token) =>
        {
            if (parts.Length >= 4 && bool.TryParse(parts[3], out var isEnabled))
            {
                await tenantFlagManager.SetFlagAsync(parts[1], parts[2], isEnabled, token).ConfigureAwait(false);
            }
            else
            {
                ConsoleHelper.WriteLineLocked("Usage: set <tenantId> <flagName> <true|false>");
                await Task.Delay(1500, token).ConfigureAwait(false);
            }
        });

        app.RegisterCommand("del", "Removes a flag from any tenant shard (e.g., del tenant-1 NewBilling)", async (parts, token) =>
        {
            if (parts.Length >= 3)
            {
                await tenantFlagManager.RemoveFlagAsync(parts[1], parts[2], token).ConfigureAwait(false);
            }
            else
            {
                ConsoleHelper.WriteLineLocked("Usage: del <tenantId> <flagName>");
                await Task.Delay(1500, token).ConfigureAwait(false);
            }
        });

        app.RegisterCommand("read", "Reads a tenant's flags (transparently fetches via RPC if hosted remotely)", async (parts, token) =>
        {
            if (parts.Length >= 2)
            {
                var tenantId = parts[1];
                var flags = await tenantFlagManager.GetTenantFlagsAsync(tenantId, token).ConfigureAwait(false);
                ConsoleHelper.WriteLineLocked($"\n--- [Cluster Read (Local or RPC Proxy) for '{tenantId}'] ---");
                if (flags.Count == 0)
                {
                    ConsoleHelper.WriteLineLocked(" (No flags found or document does not exist)");
                }
                else
                {
                    foreach (var kvp in flags.OrderBy(f => f.Key, StringComparer.Ordinal))
                    {
                        ConsoleHelper.WriteLineLocked($" => [{kvp.Key}]: {(kvp.Value ? "ENABLED" : "DISABLED")}");
                    }
                }

                ConsoleHelper.WriteLineLocked("------------------------------------------------------------");
                await Task.Delay(2500, token).ConfigureAwait(false);
            }
            else
            {
                ConsoleHelper.WriteLineLocked("Usage: read <tenantId>");
                await Task.Delay(1500, token).ConfigureAwait(false);
            }
        });

        app.RegisterCommand("drop", "Deletes an entire tenant document from the cluster (e.g., drop tenant-1)", async (parts, token) =>
        {
            if (parts.Length >= 2)
            {
                await tenantFlagManager.DeleteTenantAsync(parts[1], token).ConfigureAwait(false);
            }
            else
            {
                ConsoleHelper.WriteLineLocked("Usage: drop <tenantId>");
                await Task.Delay(1500, token).ConfigureAwait(false);
            }
        });

        app.SetStateRenderer(async () =>
        {
            var overview = await tenantFlagManager.GetClusterShardOverviewAsync(cts.Token).ConfigureAwait(false);
            var localCount = overview.Count(o => o.IsHostedLocally);
            var remoteCount = overview.Count - localCount;

            Console.WriteLine($"\n--- [Consistent Hash Ring & Local Shard State: {replicaId} (RF={replicationFactor})] ---");
            Console.WriteLine($" Cluster Documents: {overview.Count} total | Hosted Locally: {localCount} | Hosted Remotely: {remoteCount}");
            Console.WriteLine("-----------------------------------------------------------------------------------------------");

            if (overview.Count == 0)
            {
                Console.WriteLine(" (No tenant documents registered yet. Type 'seed' or 'set tenant-1 MyFlag true')");
            }
            else
            {
                foreach (var item in overview)
                {
                    var owners = item.AssignedReplicas.Count > 0 ? string.Join(", ", item.AssignedReplicas) : replicaId;
                    var placementTag = item.IsHostedLocally ? "LOCAL " : "REMOTE";

                    if (item.IsHostedLocally)
                    {
                        var flagsSummary = item.LocalFlags.Count == 0
                            ? "(empty)"
                            : string.Join(", ", item.LocalFlags.OrderBy(f => f.Key, StringComparer.Ordinal).Select(f => $"{f.Key}={(f.Value ? "ON" : "OFF")}"));

                        Console.WriteLine($" [{placementTag}] Doc: {item.DocumentId,-10} | Shard: {item.ShardId,-21} | Owners: [{owners}] | Mem: {item.WholeDocumentBytes} B (DVV: {item.ShardDvvBytes} B)");
                        Console.WriteLine($"          Flags: {flagsSummary}");
                    }
                    else
                    {
                        Console.WriteLine($" [{placementTag}] Doc: {item.DocumentId,-10} | Shard: {item.ShardId,-21} | Owners: [{owners}] | (Not loaded in local RAM; use 'read {item.DocumentId}' or 'set {item.DocumentId} ...')");
                    }
                }
            }

            Console.WriteLine("-----------------------------------------------------------------------------------------------\n> ");
        });

        await host.StartAsync(cts.Token).ConfigureAwait(false);

        logger.LogInformation("Starting Sharded CRDT Node '{ReplicaId}' on port {Port} (ReplicationFactor={ReplicationFactor})...", replicaId, currentPort, replicationFactor);

        await app.RunAsync().ConfigureAwait(false);

        logger.LogInformation("Shutting down sharded node gracefully...");

        await host.StopAsync(CancellationToken.None).ConfigureAwait(false);
    }
}
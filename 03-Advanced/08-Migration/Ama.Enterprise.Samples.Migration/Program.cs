namespace Ama.Enterprise.Samples.Migration;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ama.Enterprise.CRDT.Distributed.Extensions;
using Ama.Enterprise.CRDT.Distributed.Services;
using Ama.Enterprise.FeatureFlags.Extensions;
using Ama.Enterprise.FeatureFlags.Models;
using Ama.Enterprise.FeatureFlags.Services;
using Ama.Enterprise.Licensing.Extensions;
using Ama.Enterprise.P2p.Extensions;
using Ama.Enterprise.Samples.Common.Logging;
using Ama.Enterprise.Samples.Common.Network;
using Ama.Enterprise.Samples.Common.UI;
using Ama.Enterprise.Samples.Migration.Extensions;
using Ama.Enterprise.Samples.Migration.Models;
using Ama.Enterprise.Samples.Migration.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        var mode = args.Length > 0 ? args[0].Trim().ToLowerInvariant() : "v1";
        if (mode is not ("v1" or "v2" or "migrator"))
        {
            mode = "v1";
        }

        var basePort = 8100;
        if (args.Length > 1 && int.TryParse(args[1], out var parsedPort))
        {
            basePort = parsedPort;
        }

        var primaryPort = NetworkUtilities.GetNextAvailablePort(basePort);
        var v1ReplicaId = $"node-v1-{primaryPort}";
        var v2ReplicaId = $"node-v2-{primaryPort}";

        // 2. Configure Distributed CRDT Core settings.
        builder.Services.AddDistributedCrdtCore(options =>
        {
            options.ActiveSyncEnabled = true;
            options.CheckpointIntervalSeconds = (int)TimeSpan.FromHours(1).TotalSeconds;
            options.AntiEntropyIntervalSeconds = 5;
            options.AntiEntropyInitialDelaySeconds = 2;
        });

        builder.Services.AddSingleton<CancellationTokenSource>();
        builder.Services.AddSingleton<InteractiveConsoleApp>();

        var nextPortCursor = primaryPort;

        // 3. Configure V1 Legacy Mesh & Replica (for 'v1' and 'migrator' nodes).
        if (mode is "v1" or "migrator")
        {
            builder.Services.AddDistributedCrdtReplica(v1ReplicaId, Constants.V1MeshName);
            builder.Services.AddSingleton<IFeatureFlagStateObserver, ConsoleFeatureFlagObserver>();
            builder.Services.AddFeatureFlags();

            var v1TcpPort = nextPortCursor;
            var v1HandshakePort = NetworkUtilities.GetNextAvailablePort(v1TcpPort + 100);
            nextPortCursor = NetworkUtilities.GetNextAvailablePort(v1TcpPort + 1);

            ConfigureMesh(
                builder.Services,
                Constants.V1MeshName,
                v1ReplicaId,
                v1TcpPort,
                v1HandshakePort,
                Constants.V1MulticastPort);
        }

        // 4. Configure V2 Modern Mesh & Replica (for 'v2' and 'migrator' nodes).
        if (mode is "v2" or "migrator")
        {
            builder.Services.AddDistributedCrdtReplica(v2ReplicaId, Constants.V2MeshName);
            builder.Services.AddSingleton<ConsoleFeatureFlagV2Observer>();
            builder.Services.AddFeatureFlagsV2();

            var v2TcpPort = nextPortCursor;
            var v2HandshakePort = NetworkUtilities.GetNextAvailablePort(v2TcpPort + 100);

            ConfigureMesh(
                builder.Services,
                Constants.V2MeshName,
                v2ReplicaId,
                v2TcpPort,
                v2HandshakePort,
                Constants.V2MulticastPort);
        }

        // 5. Register the Live CRDT Migration Bridge on the 'migrator' node.
        if (mode == "migrator")
        {
            // Partition the V1 and V2 CRDT scopes on the mediator node so that V1 state and gossip
            // only flow to/from V1 mesh peers, and V2 state and gossip only flow to/from V2 mesh peers.
            builder.Services.AddSingleton(new ReplicaMeshMapping(v1ReplicaId, Constants.V1MeshName));
            builder.Services.AddSingleton(new ReplicaMeshMapping(v2ReplicaId, Constants.V2MeshName));
            builder.Services.Replace(ServiceDescriptor.Scoped<IScopeTopologyProvider, MigrationScopeTopologyProvider>());

            builder.Services.AddDistributedCrdtMigration<FeatureFlagState, FeatureFlagStateV2, FeatureFlagV1ToV2Migrator>(options =>
            {
                options.MigrationId = Constants.MigrationId;
                options.SourceReplicaId = v1ReplicaId;
                options.SourceTypeAlias = Ama.Enterprise.FeatureFlags.Constants.FeatureFlagDocumentType;
                options.TargetReplicaId = v2ReplicaId;
                options.TargetTypeAlias = Constants.FeatureFlagV2DocumentType;
                options.BackfillBatchDelayMilliseconds = 10;
                options.ReconciliationScanIntervalSeconds = 2;
            });
        }

        using var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("MigrationSample");
        var scopeManager = host.Services.GetRequiredService<DistributedCrdtScopeManager>();

        IFeatureFlagCrdtManager? v1Manager = null;
        IFeatureFlagV2CrdtManager? v2Manager = null;

        if (mode is "v1" or "migrator")
        {
            var v1Scope = scopeManager.GetOrCreateScope(v1ReplicaId);
            v1Manager = v1Scope.ServiceProvider.GetRequiredService<IFeatureFlagCrdtManager>();
        }

        if (mode is "v2" or "migrator")
        {
            var v2Scope = scopeManager.GetOrCreateScope(v2ReplicaId);
            v2Manager = v2Scope.ServiceProvider.GetRequiredService<IFeatureFlagV2CrdtManager>();
        }

        var cts = host.Services.GetRequiredService<CancellationTokenSource>();
        var app = host.Services.GetRequiredService<InteractiveConsoleApp>();

        var modeDescription = mode switch
        {
            "v1" => $"Legacy V1 Node ({Constants.V1MeshName} on UDP {Constants.V1MulticastPort})",
            "v2" => $"Modern V2 Node ({Constants.V2MeshName} on UDP {Constants.V2MulticastPort})",
            _ => $"Live Migrator Bridge ({Constants.V1MeshName} [V1] ---> {Constants.V2MeshName} [V2])"
        };

        app.SetTitle($"Live Schema Migration Showcase [{mode.ToUpperInvariant()}]");
        app.SetCurrentPort(primaryPort);
        app.SetIntroText($"Topology: {modeDescription}\nUsage: dotnet run -- <v1|v2|migrator> [port]");

        RegisterCommands(app, mode, v1Manager, v2Manager, host.Services);
        ConfigureStateRenderer(app, mode, v1Manager, v2Manager, host.Services);

        await host.StartAsync(cts.Token).ConfigureAwait(false);

        logger.LogInformation("Started Migration Node in '{Mode}' mode on base port {Port}...", mode, primaryPort);

        await app.RunAsync().ConfigureAwait(false);

        logger.LogInformation("Shutting down network services gracefully...");

        await host.StopAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private static void RegisterCommands(
        InteractiveConsoleApp app,
        string mode,
        IFeatureFlagCrdtManager? v1Manager,
        IFeatureFlagV2CrdtManager? v2Manager,
        IServiceProvider services)
    {
        if (v1Manager is not null && mode is "v1" or "migrator")
        {
            var cmdName = mode == "migrator" ? "set-v1" : "set";
            var delName = mode == "migrator" ? "del-v1" : "del";

            app.RegisterCommand(cmdName, $"Sets a V1 boolean flag (e.g., {cmdName} DarkMode true [owner])", async (parts, token) =>
            {
                if (parts.Length >= 3 && bool.TryParse(parts[2], out var isEnabled))
                {
                    var owner = parts.Length >= 4 ? parts[3] : "v1-user";
                    var ownership = new FeatureFlagOwnership { Owner = owner };
                    await v1Manager.SetFlagAsync(parts[1], isEnabled, modifiedBy: owner, ownership: ownership, cancellationToken: token).ConfigureAwait(false);
                }
                else
                {
                    ConsoleHelper.WriteLineLocked($"Usage: {cmdName} <name> <true|false> [owner]");
                    await Task.Delay(1500, token).ConfigureAwait(false);
                }
            });

            app.RegisterCommand(delName, $"Removes a V1 flag (e.g., {delName} DarkMode)", async (parts, token) =>
            {
                if (parts.Length >= 2)
                {
                    await v1Manager.RemoveFlagAsync(parts[1], token).ConfigureAwait(false);
                }
                else
                {
                    ConsoleHelper.WriteLineLocked($"Usage: {delName} <name>");
                    await Task.Delay(1500, token).ConfigureAwait(false);
                }
            });
        }

        if (v2Manager is not null && mode is "v2" or "migrator")
        {
            var cmdName = mode == "migrator" ? "set-v2" : "set";
            var delName = mode == "migrator" ? "del-v2" : "del";

            app.RegisterCommand(cmdName, $"Sets a V2 rollout rule 0-100% (e.g., {cmdName} NewCheckout 50 [owner])", async (parts, token) =>
            {
                if (parts.Length >= 3 && int.TryParse(parts[2], out var percentage))
                {
                    var owner = parts.Length >= 4 ? parts[3] : "v2-user";
                    await v2Manager.SetRuleAsync(parts[1], percentage, owner: owner, cancellationToken: token).ConfigureAwait(false);
                }
                else
                {
                    ConsoleHelper.WriteLineLocked($"Usage: {cmdName} <key> <0..100> [owner]");
                    await Task.Delay(1500, token).ConfigureAwait(false);
                }
            });

            app.RegisterCommand(delName, $"Removes a V2 rule (e.g., {delName} NewCheckout)", async (parts, token) =>
            {
                if (parts.Length >= 2)
                {
                    await v2Manager.RemoveRuleAsync(parts[1], token).ConfigureAwait(false);
                }
                else
                {
                    ConsoleHelper.WriteLineLocked($"Usage: {delName} <key>");
                    await Task.Delay(1500, token).ConfigureAwait(false);
                }
            });
        }

        if (mode == "migrator")
        {
            app.RegisterCommand("drain", "Drains and flushes all pending V1 -> V2 migrations", async (_, token) =>
            {
                var migrationManager = services.GetRequiredService<ICrdtMigrationManager>();
                await migrationManager.DrainAllAsync(token).ConfigureAwait(false);
                app.RequestRedraw();
            });
        }
    }

    private static void ConfigureStateRenderer(
        InteractiveConsoleApp app,
        string mode,
        IFeatureFlagCrdtManager? v1Manager,
        IFeatureFlagV2CrdtManager? v2Manager,
        IServiceProvider services)
    {
        app.SetStateRenderer(async () =>
        {
            if (mode == "migrator")
            {
                var migrationManager = services.GetRequiredService<ICrdtMigrationManager>();
                if (migrationManager.TryGetStatus(Constants.MigrationId, out var status))
                {
                    Console.WriteLine($"\n--- [Migration Pipeline: {status.MigrationId}] ---");
                    Console.WriteLine($" Converged : {status.IsConverged} | Backfilled: {status.BackfilledDocuments} | Pending Queue: {status.PendingDocumentsInQueue} | Failed: {status.FailedTranslations}");
                }
            }

            if (v1Manager is not null)
            {
                var v1Flags = await v1Manager.GetFlagsAsync().ConfigureAwait(false);
                Console.WriteLine("\n--- [Legacy V1 State (FeatureFlagState.Flags)] ---");
                if (v1Flags.Count == 0)
                {
                    Console.WriteLine(" (No V1 flags defined)");
                }
                else
                {
                    foreach (var flag in v1Flags.OrderBy(f => f.Key, StringComparer.Ordinal))
                    {
                        var owner = flag.Value.Ownership?.Owner ?? flag.Value.Audit?.LastModifiedBy ?? "n/a";
                        Console.WriteLine($" => [V1] {flag.Key}: IsEnabled={flag.Value.IsEnabled} (Owner: {owner})");
                    }
                }
            }

            if (v2Manager is not null)
            {
                var v2Rules = await v2Manager.GetRulesAsync().ConfigureAwait(false);
                Console.WriteLine("\n--- [Modern V2 State (FeatureFlagStateV2.Rules)] ---");
                if (v2Rules.Count == 0)
                {
                    Console.WriteLine(" (No V2 rules defined)");
                }
                else
                {
                    foreach (var rule in v2Rules.OrderBy(r => r.Key, StringComparer.Ordinal))
                    {
                        Console.WriteLine($" => [V2] {rule.Key}: State={rule.Value.State} ({rule.Value.RolloutPercentage}%) | Owner={rule.Value.Owner} | Schema=v{rule.Value.SchemaVersion}");
                    }
                }
            }

            Console.WriteLine("--------------------------------------------------\n> ");
        });
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
namespace Ama.Enterprise.Samples.MultipleReplicas;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

internal sealed class Program
{
    public static async Task Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var basePort = Constants.DefaultListenPort;
        if (args.Length > 0 && int.TryParse(args[0], out var parsedPort))
        {
            basePort = parsedPort;
        }

        // Allocate distinct TCP and UDP handshake ports for each replica's dedicated P2P mesh.
        var stagingTcpPort = NetworkUtilities.GetNextAvailablePort(basePort);
        var stagingHandshakePort = NetworkUtilities.GetNextAvailablePort(stagingTcpPort + 100);

        var prodTcpPort = NetworkUtilities.GetNextAvailablePort(stagingTcpPort + 1);
        var prodHandshakePort = NetworkUtilities.GetNextAvailablePort(prodTcpPort + 100);

        var stagingReplicaId = $"staging-node-{stagingTcpPort}";
        var prodReplicaId = $"prod-node-{prodTcpPort}";

        // Share the interactive console UI, logger provider, and cancellation token across both isolated hosts.
        using var cts = new CancellationTokenSource();
        using var loggerProvider = new LockedConsoleLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddFilter("Microsoft", LogLevel.Warning);
            logging.AddFilter("System", LogLevel.Warning);
            logging.ClearProviders();
            logging.AddProvider(loggerProvider);
        });

        var logger = loggerFactory.CreateLogger("MultiReplicaSample");
        var app = new InteractiveConsoleApp(loggerFactory, cts);

        // Build two completely separate ServiceCollections / IHosts.
        // Because the P2P Gossip algorithm and local CRDT broadcast bus are global to an IServiceCollection
        // (which is how a single container bridges multiple meshes), true multi-tenant / multi-environment
        // replica isolation in a single process requires separate IServiceCollection / IHost containers.
        using var stagingHost = BuildIsolatedReplicaHost(
            args,
            loggerProvider,
            app,
            Constants.StagingMeshName,
            stagingReplicaId,
            stagingTcpPort,
            stagingHandshakePort,
            Constants.StagingMulticastPort);

        using var prodHost = BuildIsolatedReplicaHost(
            args,
            loggerProvider,
            app,
            Constants.ProductionMeshName,
            prodReplicaId,
            prodTcpPort,
            prodHandshakePort,
            Constants.ProductionMulticastPort);

        // Resolve the isolated CRDT scope and IFeatureFlagCrdtManager from each independent host container.
        var stagingScope = stagingHost.Services
            .GetRequiredService<DistributedCrdtScopeManager>()
            .GetOrCreateScope(stagingReplicaId);
        var prodScope = prodHost.Services
            .GetRequiredService<DistributedCrdtScopeManager>()
            .GetOrCreateScope(prodReplicaId);

        var stagingManager = stagingScope.ServiceProvider.GetRequiredService<IFeatureFlagCrdtManager>();
        var prodManager = prodScope.ServiceProvider.GetRequiredService<IFeatureFlagCrdtManager>();

        app.SetTitle("Multi-Tenant / Multi-Replica Feature Flags Showcase");
        app.SetCurrentPort(stagingTcpPort);
        app.SetIntroText(
            "Hosting 2 isolated replicas in one process (each in its own isolated IHost / ServiceCollection):\n" +
            $"  - STAGING Replica    : {stagingReplicaId} -> Mesh '{Constants.StagingMeshName}' (TCP {stagingTcpPort}, UDP {Constants.StagingMulticastPort})\n" +
            $"  - PRODUCTION Replica : {prodReplicaId} -> Mesh '{Constants.ProductionMeshName}' (TCP {prodTcpPort}, UDP {Constants.ProductionMulticastPort})");

        // Define console commands to interact with either replica or promote flags between them.
        app.RegisterCommand("set", "Sets a flag in a tenant/env scope: set <staging|prod> <FlagName> <true|false>", async (parts, token) =>
        {
            if (parts.Length >= 4 &&
                TryResolveManager(parts[1], stagingManager, prodManager, out var targetManager, out var scopeLabel) &&
                bool.TryParse(parts[3], out var isEnabled))
            {
                await targetManager.SetFlagAsync(parts[2], isEnabled, cancellationToken: token).ConfigureAwait(false);
                logger.LogInformation("Updated flag '{Flag}' to {State} in [{Scope}] replica.", parts[2], isEnabled, scopeLabel);
            }
            else
            {
                ConsoleHelper.WriteLineLocked("Usage: set <staging|prod> <FlagName> <true|false>");
                await Task.Delay(1500, token).ConfigureAwait(false);
            }
        });

        app.RegisterCommand("del", "Removes a flag from a tenant/env scope: del <staging|prod> <FlagName>", async (parts, token) =>
        {
            if (parts.Length >= 3 &&
                TryResolveManager(parts[1], stagingManager, prodManager, out var targetManager, out var scopeLabel))
            {
                await targetManager.RemoveFlagAsync(parts[2], token).ConfigureAwait(false);
                logger.LogInformation("Removed flag '{Flag}' from [{Scope}] replica.", parts[2], scopeLabel);
            }
            else
            {
                ConsoleHelper.WriteLineLocked("Usage: del <staging|prod> <FlagName>");
                await Task.Delay(1500, token).ConfigureAwait(false);
            }
        });

        app.RegisterCommand("promote", "Promotes a flag from STAGING to PRODUCTION: promote <FlagName>", async (parts, token) =>
        {
            if (parts.Length >= 2)
            {
                var flagName = parts[1];
                var stagingFlags = await stagingManager.GetFlagsAsync(token).ConfigureAwait(false);
                if (stagingFlags.TryGetValue(flagName, out var flagSetting))
                {
                    await prodManager.SetFlagAsync(flagName, flagSetting.IsEnabled, cancellationToken: token).ConfigureAwait(false);
                    logger.LogInformation("Promoted flag '{Flag}' ({State}) from STAGING to PRODUCTION.", flagName, flagSetting.IsEnabled);
                }
                else
                {
                    ConsoleHelper.WriteLineLocked($"Flag '{flagName}' does not exist in the STAGING replica.");
                    await Task.Delay(1500, token).ConfigureAwait(false);
                }
            }
            else
            {
                ConsoleHelper.WriteLineLocked("Usage: promote <FlagName>");
                await Task.Delay(1500, token).ConfigureAwait(false);
            }
        });

        // Define how both isolated replica states are displayed side-by-side in the console.
        app.SetStateRenderer(async () =>
        {
            var stagingFlags = await stagingManager.GetFlagsAsync().ConfigureAwait(false);
            var prodFlags = await prodManager.GetFlagsAsync().ConfigureAwait(false);

            Console.WriteLine("\n--- [Multi-Replica State (Isolated ServiceCollections / Hosts)] ---");
            RenderScopeFlags($"STAGING ({stagingReplicaId} @ {Constants.StagingMeshName})", stagingFlags);
            Console.WriteLine();
            RenderScopeFlags($"PRODUCTION ({prodReplicaId} @ {Constants.ProductionMeshName})", prodFlags);
            Console.WriteLine("--------------------------------------------------------------------\n> ");
        });

        // Start both isolated replica hosts before entering the UI loop.
        await stagingHost.StartAsync(cts.Token).ConfigureAwait(false);
        await prodHost.StartAsync(cts.Token).ConfigureAwait(false);

        logger.LogInformation(
            "Started Multi-Replica Node with isolated containers for '{StagingReplica}' (TCP {StagingPort}) and '{ProdReplica}' (TCP {ProdPort})...",
            stagingReplicaId,
            stagingTcpPort,
            prodReplicaId,
            prodTcpPort);

        // Run the interactive console application.
        await app.RunAsync().ConfigureAwait(false);

        logger.LogInformation("Shutting down isolated replica hosts gracefully...");

        await prodHost.StopAsync(CancellationToken.None).ConfigureAwait(false);
        await stagingHost.StopAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private static IHost BuildIsolatedReplicaHost(
        string[] args,
        ILoggerProvider loggerProvider,
        InteractiveConsoleApp sharedApp,
        string meshName,
        string replicaId,
        int tcpListenPort,
        int udpHandshakePort,
        int multicastPort)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(loggerProvider);
        ArgumentNullException.ThrowIfNull(sharedApp);
        ArgumentException.ThrowIfNullOrWhiteSpace(meshName);
        ArgumentException.ThrowIfNullOrWhiteSpace(replicaId);

        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args
        });

        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
        builder.Logging.AddFilter("System", LogLevel.Warning);
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(loggerProvider);

        // 1. Configure Open Source Licensing for this container.
        builder.Services.ConfigureAmaCommunityLicense();

        // 2. Register Distributed CRDT Core for this isolated replica container.
        builder.Services.AddDistributedCrdtCore(options =>
        {
            options.ActiveSyncEnabled = true;
            options.CheckpointIntervalSeconds = (int)TimeSpan.FromHours(1).TotalSeconds;
            options.AntiEntropyIntervalSeconds = 5;
            options.AntiEntropyInitialDelaySeconds = 3;
        });

        // 3. Register Feature Flags and forward state change notifications to the shared console UI.
        builder.Services.AddSingleton(sharedApp);
        builder.Services.AddSingleton<IFeatureFlagStateObserver, ConsoleFeatureFlagObserver>();
        builder.Services.AddFeatureFlags();

        // 4. Link the P2P mesh to this container's replica and configure its dedicated network ports.
        builder.Services.AddDistributedCrdtReplica(replicaId, meshName);

        builder.Services.AddP2pMesh(meshName)
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

        return builder.Build();
    }

    private static bool TryResolveManager(
        string scopeInput,
        IFeatureFlagCrdtManager stagingManager,
        IFeatureFlagCrdtManager prodManager,
        out IFeatureFlagCrdtManager manager,
        out string scopeLabel)
    {
        if (string.Equals(scopeInput, Constants.StagingScopeKey, StringComparison.OrdinalIgnoreCase))
        {
            manager = stagingManager;
            scopeLabel = "STAGING";
            return true;
        }

        if (string.Equals(scopeInput, Constants.ProductionScopeKey, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(scopeInput, "production", StringComparison.OrdinalIgnoreCase))
        {
            manager = prodManager;
            scopeLabel = "PRODUCTION";
            return true;
        }

        manager = stagingManager;
        scopeLabel = string.Empty;
        return false;
    }

    private static void RenderScopeFlags(string header, IReadOnlyDictionary<string, FeatureFlag> flags)
    {
        Console.WriteLine($" [{header}]");
        if (flags.Count == 0)
        {
            Console.WriteLine("   (No flags currently defined)");
            return;
        }

        foreach (var flag in flags.OrderBy(f => f.Key))
        {
            Console.WriteLine($"   => [{flag.Key}]: {(flag.Value.IsEnabled ? "ENABLED" : "DISABLED")}");
        }
    }
}
namespace Ama.Enterprise.Samples.ZeroTrustRouting;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ama.Enterprise.CRDT.Distributed.Extensions;
using Ama.Enterprise.CRDT.Distributed.Services;
using Ama.Enterprise.FeatureFlags.Extensions;
using Ama.Enterprise.FeatureFlags.Services;
using Ama.Enterprise.Licensing.Extensions;
using Ama.Enterprise.P2p.Extensions;
using Ama.Enterprise.Samples.Common.Logging;
using Ama.Enterprise.Samples.Common.Network;
using Ama.Enterprise.Samples.Common.UI;
using Ama.Enterprise.Samples.ZeroTrustRouting.Models;
using Ama.Enterprise.Samples.ZeroTrustRouting.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        // 1. Configure Open Source Community Licensing.
        builder.Services.ConfigureAmaCommunityLicense();

        // Parse CLI arguments: [port] [role] [region]
        // Example: dotnet run -- 8100 admin EU
        var currentPort = Constants.DefaultStartingPort;
        if (args.Length > 0 && int.TryParse(args[0], out var parsedPort) && parsedPort > 0)
        {
            currentPort = parsedPort;
        }
        else
        {
            currentPort = NetworkUtilities.GetNextAvailablePort(currentPort);
        }

        var role = args.Length > 1 && !string.IsNullOrWhiteSpace(args[1])
            ? args[1].Trim().ToLowerInvariant()
            : Constants.DefaultRole;

        var region = args.Length > 2 && !string.IsNullOrWhiteSpace(args[2])
            ? args[2].Trim().ToUpperInvariant()
            : Constants.DefaultRegion;

        var handshakePort = NetworkUtilities.GetNextAvailablePort(currentPort + 100);
        var replicaId = $"node-{currentPort}";
        var sessionContext = new NodeSessionContext(role, region);

        // Register the local node's session identity (Role & Region).
        builder.Services.AddSingleton(sessionContext);

        // 2. Register the CRDT replica and core distributed settings.
        builder.Services.AddDistributedCrdtCore(options =>
        {
            options.ActiveSyncEnabled = true;
            options.CheckpointIntervalSeconds = (int)TimeSpan.FromHours(1).TotalSeconds;
            options.AntiEntropyIntervalSeconds = 5;
            options.AntiEntropyInitialDelaySeconds = 3;
        });

        // 3. Replace the default topology provider with our session-aware RBAC & Geo-Fencing provider.
        // This ensures background Anti-Entropy and DVV tracking respect the exact same boundaries as Gossip.
        builder.Services.Replace(ServiceDescriptor.Scoped<IScopeTopologyProvider, RbacScopeTopologyProvider>());

        // 4. Link the P2P mesh network to the CRDT replica.
        builder.Services.AddDistributedCrdtReplica(replicaId, Constants.MeshName);

        // 5. Add Feature Flags and UI services.
        builder.Services.AddSingleton<CancellationTokenSource>();
        builder.Services.AddSingleton<InteractiveConsoleApp>();
        builder.Services.AddSingleton<IFeatureFlagStateObserver, ConsoleFeatureFlagObserver>();
        builder.Services.AddFeatureFlags();

        // 6. Configure the P2P Mesh with Session Authentication and Zero-Trust Routing over TCP/UDP.
        builder.Services.AddP2pMesh(Constants.MeshName)
            .AddSessionAuthentication<RbacSessionTokenValidator>(options =>
            {
                options.RequireSession = true;
            })
            .EnableZeroTrustRouting()
            .AddRoutingPolicy<RbacMeshRoutingPolicy>()
            .AddGossipNetwork(options =>
            {
                options.GossipInterval = TimeSpan.FromMilliseconds(1000);
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
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("SessionSample");

        var scopeManager = host.Services.GetRequiredService<DistributedCrdtScopeManager>();
        var crdtScope = scopeManager.GetOrCreateScope(replicaId);
        var clusterManager = crdtScope.ServiceProvider.GetRequiredService<IFeatureFlagCrdtManager>();

        var cts = host.Services.GetRequiredService<CancellationTokenSource>();
        var app = host.Services.GetRequiredService<InteractiveConsoleApp>();

        app.SetTitle($"Zero-Trust Session Management [Role: {sessionContext.Role.ToUpperInvariant()} | Region: {sessionContext.Region}]");
        app.SetCurrentPort(currentPort);
        app.SetIntroText(
            $"Local Claims => Role: '{sessionContext.Role}', Region: '{sessionContext.Region}'.\n" +
            "Rules:\n" +
            "  1. RBAC Isolation: 'admin' and 'user' nodes only sync with matching roles (or 'server' hubs).\n" +
            "  2. Geo-Fencing: 'US' data flows into 'EU', but 'EU' data never leaves 'EU'.");

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

        app.RegisterCommand("spawn", "Spawns a new node with role & region (e.g., spawn admin EU)", async (parts, token) =>
        {
            var targetRole = parts.Length >= 2 ? parts[1].ToLowerInvariant() : sessionContext.Role;
            var targetRegion = parts.Length >= 3 ? parts[2].ToUpperInvariant() : sessionContext.Region;

            SpawnNodeProcess(currentPort, targetRole, targetRegion, logger);
            await Task.Delay(500, token).ConfigureAwait(false);
        });

        app.SetStateRenderer(async () =>
        {
            var flags = await clusterManager.GetFlagsAsync().ConfigureAwait(false);
            Console.WriteLine($"\n--- [Synchronized State | Role: {sessionContext.Role.ToUpperInvariant()} | Region: {sessionContext.Region}] ---");
            if (flags.Count == 0)
            {
                Console.WriteLine(" (No flags currently defined in this partition)");
            }
            else
            {
                foreach (var flag in flags.OrderBy(f => f.Key))
                {
                    Console.WriteLine($" => [{flag.Key}]: {(flag.Value.IsEnabled ? "ENABLED" : "DISABLED")}");
                }
            }
            Console.WriteLine("------------------------------------------------------------------\n> ");
        });

        await host.StartAsync(cts.Token).ConfigureAwait(false);

        logger.LogInformation(
            "Started Node on TCP port {Port} with Session Claims [Role={Role}, Region={Region}].",
            currentPort,
            sessionContext.Role,
            sessionContext.Region);

        await app.RunAsync().ConfigureAwait(false);

        logger.LogInformation("Shutting down network services gracefully...");

        await host.StopAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private static void SpawnNodeProcess(int currentPort, string role, string region, ILogger logger)
    {
        var nextPort = NetworkUtilities.GetNextAvailablePort(currentPort + 1);
        var processPath = Environment.ProcessPath;

        if (string.IsNullOrEmpty(processPath))
        {
            logger.LogWarning("Unable to determine executable path to spawn a new node.");
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = processPath,
            Arguments = $"{nextPort} {role} {region}",
            UseShellExecute = true
        });

        logger.LogInformation(
            "Spawned new node on port {NextPort} with Role='{Role}' and Region='{Region}'.",
            nextPort,
            role,
            region);
    }
}
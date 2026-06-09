namespace Ama.Enterprise.Samples.Serialization;

using Ama.CRDT.Extensions;
using Ama.CRDT.Services.Serialization;
using Ama.Enterprise.CRDT.Distributed.Extensions;
using Ama.Enterprise.CRDT.Distributed.Services;
using Ama.Enterprise.CRDT.MessagePack.Extensions;
using Ama.Enterprise.CRDT.MessagePack.Resolvers;
using Ama.Enterprise.FeatureFlags.Extensions;
using Ama.Enterprise.FeatureFlags.Services;
using Ama.Enterprise.Licensing.Extensions;
using Ama.Enterprise.P2p.Extensions;
using Ama.Enterprise.Samples.Common.Logging;
using Ama.Enterprise.Samples.Common.Network;
using Ama.Enterprise.Samples.Common.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

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

        // 1. Configure Licensing.
        // Note: AddCrdtSystemTextJson works with Community license, while AddCrdtMessagePack requires Enterprise license.
        builder.Services.ConfigureAmaCommunityLicense();

        var serializerMode = ParseSerializerMode(args);
        var currentPort = ParseListenPort(args);
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
        builder.Services.AddDistributedCrdtReplica(replicaId, Constants.MeshName);

        // 4. Add the Feature Flags services and register the console state observer via DI.
        builder.Services.AddSingleton<CancellationTokenSource>();
        builder.Services.AddSingleton<InteractiveConsoleApp>();
        builder.Services.AddSingleton<IFeatureFlagStateObserver, ConsoleFeatureFlagObserver>();
        builder.Services.AddFeatureFlags();

        // 5. Configure the CRDT Serialization strategy based on the selected mode:
        //    - "json"    : Standard System.Text.Json (UTF-8 JSON)
        //    - "brotli"  : System.Text.Json with Brotli compression
        //    - "msgpack" : Binary MessagePack serialization with AOT source-generated resolvers
        ConfigureCrdtSerialization(builder.Services, serializerMode);

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
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("SerializationSample");

        var activeSerializer = host.Services.GetRequiredService<ICrdtSerializer>();

        // Get the feature flag cluster manager and orchestrator for this node.
        var scopeManager = host.Services.GetRequiredService<DistributedCrdtScopeManager>();
        var crdtScope = scopeManager.GetOrCreateScope(replicaId);
        var clusterManager = crdtScope.ServiceProvider.GetRequiredService<IFeatureFlagCrdtManager>();
        var orchestrator = crdtScope.ServiceProvider.GetRequiredService<ICrdtDocumentOrchestrator>();

        var cts = host.Services.GetRequiredService<CancellationTokenSource>();
        var app = host.Services.GetRequiredService<InteractiveConsoleApp>();
        app.SetTitle($"CRDT Serialization Showcase [{serializerMode.ToUpperInvariant()} / {activeSerializer.GetType().Name}]");
        app.SetCurrentPort(currentPort);
        app.SetIntroText(
            $"Active ICrdtSerializer: {activeSerializer.GetType().Name} (mode: {serializerMode}). " +
            "All nodes in the same mesh must use the same wire serialization format.");

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

        // Define how the current feature flag state and serialized footprint are displayed in the console.
        app.SetStateRenderer(async () =>
        {
            var flags = await clusterManager.GetFlagsAsync().ConfigureAwait(false);
            Console.WriteLine($"\n--- [Cluster State Synchronized ({activeSerializer.GetType().Name})] ---");
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

            Console.WriteLine("\n--- [Serialized Document Footprint] ---");
            foreach (var doc in orchestrator.GetActiveDocuments())
            {
                var footprint = await doc.EstimateMemoryFootprintAsync(cts.Token).ConfigureAwait(false);
                Console.WriteLine(
                    $" => Doc [{doc.DocumentId}]: Data={footprint.DataBytes} B | " +
                    $"Meta={footprint.MetadataBytes} B | " +
                    $"Total={footprint.WholeDocumentBytes} B | " +
                    $"DVV={footprint.ShardVersionVectorBytes} B");
            }

            Console.WriteLine("------------------------------------\n> ");
        });

        // Start the host and all background services before entering the UI loop.
        await host.StartAsync(cts.Token).ConfigureAwait(false);

        logger.LogInformation(
            "Starting CRDT Node on port {Port} using serializer {Serializer} (mode: {Mode})...",
            currentPort,
            activeSerializer.GetType().Name,
            serializerMode);

        // Run the interactive console application.
        await app.RunAsync().ConfigureAwait(false);

        logger.LogInformation("Shutting down network services gracefully...");

        await host.StopAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private static void ConfigureCrdtSerialization(IServiceCollection services, string serializerMode)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(serializerMode);

        switch (serializerMode)
        {
            case Constants.ModeJson:
                // Option 1: Plain UTF-8 System.Text.Json (human-readable, AOT-compatible).
                services.AddCrdtSystemTextJson(useBrotliCompression: false);
                break;

            case Constants.ModeBrotli:
                // Option 2: System.Text.Json compressed with Brotli (smaller wire & storage payloads, AOT-compatible).
                services.AddCrdtSystemTextJson(useBrotliCompression: true);
                break;

            case Constants.ModeMessagePack:
                // Option 3: MessagePack binary serialization (compact binary format, AOT-compatible via generated resolvers).
                // Pass Ama_Enterprise_CRDT_MessagePack_MessagePackResolver.Instance along with any custom
                // source-generated IFormatterResolver instances for your custom CRDT models.
                services.AddCrdtMessagePack(
                    Ama_Enterprise_CRDT_MessagePack_MessagePackResolver.Instance, // Base types formatters from Ama.* projects
                    Ama_Enterprise_Samples_Serialization_MessagePackResolver.Instance); // Your project types
                break;
        }
    }

    private static string ParseSerializerMode(string[] args)
    {
        foreach (var arg in args)
        {
            if (string.Equals(arg, Constants.ModeJson, StringComparison.OrdinalIgnoreCase))
            {
                return Constants.ModeJson;
            }

            if (string.Equals(arg, Constants.ModeBrotli, StringComparison.OrdinalIgnoreCase))
            {
                return Constants.ModeBrotli;
            }

            if (string.Equals(arg, Constants.ModeMessagePack, StringComparison.OrdinalIgnoreCase))
            {
                return Constants.ModeMessagePack;
            }
        }

        return Constants.ModeBrotli;
    }

    private static int ParseListenPort(string[] args)
    {
        foreach (var arg in args)
        {
            if (int.TryParse(arg, out var parsedPort))
            {
                return parsedPort;
            }
        }

        return NetworkUtilities.GetNextAvailablePort(Constants.DefaultListenPort);
    }
}

// This is needed for the messagepack source generators to pick up the project
[JsonSerializable(typeof(bool))]
public sealed partial class ShowCaseJsonContext : JsonSerializerContext
{
}
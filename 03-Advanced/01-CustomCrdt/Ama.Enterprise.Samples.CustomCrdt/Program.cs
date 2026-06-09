namespace Ama.Enterprise.Samples.CustomCrdt;

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
using Ama.Enterprise.Samples.CustomCrdt.Extensions;
using Ama.Enterprise.Samples.CustomCrdt.Services;
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

        // 1. Configure Community Licensing.
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
        builder.Services.AddDistributedCrdtReplica(replicaId, "chat-messenger-internal-mesh");

        // 4. Register the Console UI and our Custom Chat CRDT Manager.
        builder.Services.AddSingleton<CancellationTokenSource>();
        builder.Services.AddSingleton<InteractiveConsoleApp>();
        builder.Services.AddChatMessengerCrdt();

        // 5. Configure the Mesh Network to use TCP transport and UDP discovery.
        builder.Services.AddP2pMesh("chat-messenger-internal-mesh")
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
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("ChatSample");

        // Resolve the custom chat manager for this replica scope.
        var scopeManager = host.Services.GetRequiredService<DistributedCrdtScopeManager>();
        var crdtScope = scopeManager.GetOrCreateScope(replicaId);
        var chatManager = crdtScope.ServiceProvider.GetRequiredService<IChatCrdtManager>();

        var cts = host.Services.GetRequiredService<CancellationTokenSource>();
        var app = host.Services.GetRequiredService<InteractiveConsoleApp>();
        app.SetTitle($"Custom CRDT Chat Showcase ({replicaId})");
        app.SetCurrentPort(currentPort);
        app.SetIntroText(
            $"Demonstrates a custom Chat CRDT with bounded PriorityQueue history (max {Constants.MaxChatHistoryCount}), per-replica Likes (OR-Set), and Spam moderation (ApprovalQuorum={Constants.SpamQuorumCount}).");

        // Register command to post a new chat message to the priority queue (automatically pops oldest entries beyond MaxChatHistoryCount).
        app.RegisterCommand("post", $"Posts a message to the chat priority queue (keeps latest {Constants.MaxChatHistoryCount}, e.g., post Hello mesh!)", async (parts, token) =>
        {
            if (parts.Length >= 2)
            {
                var content = string.Join(' ', parts.Skip(1));
                var posted = await chatManager.PostMessageAsync(replicaId, content, token).ConfigureAwait(false);
                ConsoleHelper.WriteLineLocked($"[{replicaId}] Posted [{posted.Id}]: \"{posted.Content}\"");
                app.RequestRedraw();
            }
            else
            {
                ConsoleHelper.WriteLineLocked("Usage: post <message text> (e.g., post Hello from my replica!)");
                await Task.Delay(1500, token).ConfigureAwait(false);
            }
        });

        // Register command to like a chat item in the priority queue (once per replica).
        app.RegisterCommand("like", "Likes an active chat message once per replica (e.g., like msg-1)", async (parts, token) =>
        {
            if (parts.Length >= 2)
            {
                var msgId = parts[1].ToLowerInvariant();
                var activeItems = await chatManager.GetChatItemsAsync(token).ConfigureAwait(false);
                if (activeItems.Any(m => string.Equals(m.Id, msgId, StringComparison.OrdinalIgnoreCase)))
                {
                    await chatManager.LikeMessageAsync(msgId, replicaId, token).ConfigureAwait(false);
                    app.RequestRedraw();
                }
                else
                {
                    ConsoleHelper.WriteLineLocked($"Unknown or popped message ID '{parts[1]}'. Active IDs: {string.Join(", ", activeItems.Select(m => m.Id))}");
                    await Task.Delay(1500, token).ConfigureAwait(false);
                }
            }
            else
            {
                ConsoleHelper.WriteLineLocked("Usage: like <msgId> (e.g., like msg-1)");
                await Task.Delay(1500, token).ConfigureAwait(false);
            }
        });

        // Register command to vote a chat item as spam using ApprovalQuorum.
        app.RegisterCommand("spam", $"Votes to mark an active message as spam; requires {Constants.SpamQuorumCount} replica votes (e.g., spam msg-3)", async (parts, token) =>
        {
            if (parts.Length >= 2)
            {
                var msgId = parts[1].ToLowerInvariant();
                var activeItems = await chatManager.GetChatItemsAsync(token).ConfigureAwait(false);
                if (activeItems.Any(m => string.Equals(m.Id, msgId, StringComparison.OrdinalIgnoreCase)))
                {
                    await chatManager.VoteSpamAsync(msgId, token).ConfigureAwait(false);
                    ConsoleHelper.WriteLineLocked($"[{replicaId}] Submitted spam vote for '{msgId}'. (Requires {Constants.SpamQuorumCount} distinct replica votes to take effect)");
                    app.RequestRedraw();
                }
                else
                {
                    ConsoleHelper.WriteLineLocked($"Unknown or popped message ID '{parts[1]}'. Active IDs: {string.Join(", ", activeItems.Select(m => m.Id))}");
                    await Task.Delay(1500, token).ConfigureAwait(false);
                }
            }
            else
            {
                ConsoleHelper.WriteLineLocked("Usage: spam <msgId> (e.g., spam msg-3)");
                await Task.Delay(1500, token).ConfigureAwait(false);
            }
        });

        // Define how the current chat state is displayed in the console.
        app.SetStateRenderer(async () =>
        {
            var items = await chatManager.GetChatItemsAsync().ConfigureAwait(false);
            Console.WriteLine($"\n--- [Chat Room Priority Queue ({items.Count}/{Constants.MaxChatHistoryCount}) | Local Replica: {replicaId}] ---");
            foreach (var item in items)
            {
                if (item.IsSpam)
                {
                    Console.WriteLine($" => [{item.Id}] ({item.Author}): [SPAM]");
                }
                else
                {
                    var likesCount = item.LikedByReplicas.Count;
                    var likedByList = likesCount > 0
                        ? $" ({string.Join(", ", item.LikedByReplicas.OrderBy(r => r, StringComparer.Ordinal))})"
                        : string.Empty;

                    Console.WriteLine($" => [{item.Id}] ({item.Author}): \"{item.Content}\" | Likes: {likesCount}{likedByList}");
                }
            }
            Console.WriteLine("------------------------------------------------------\n> ");
        });

        // Start the host and all background services before entering the UI loop.
        await host.StartAsync(cts.Token).ConfigureAwait(false);

        // Ensure the shared chat channel priority queue is initialized.
        await chatManager.EnsureChannelInitializedAsync(cts.Token).ConfigureAwait(false);

        logger.LogInformation("Starting Custom Chat CRDT Node ({ReplicaId}) on port {Port}...", replicaId, currentPort);

        // Run the interactive console application.
        await app.RunAsync().ConfigureAwait(false);

        logger.LogInformation("Shutting down network services gracefully...");

        await host.StopAsync(CancellationToken.None).ConfigureAwait(false);
    }
}
# 01 - Creating a Custom CRDT (`Ama.Enterprise.Samples.CreateCrdt`)

While `Ama.Enterprise.FeatureFlags` provides a ready-to-use distributed CRDT out of the box, the true power of `Ama.Crdt` lies in building your own custom domain models and CRDT managers.

This sample demonstrates how to build a Distributed Chat Messenger combining three distinct CRDT mechanics:
1. Bounded Chat History (`[CrdtPriorityQueueStrategy]`): Users can post live chat entries (`post <text>`) that replicate across all nodes. Messages are stored in a CRDT Priority Queue ordered by their timestamp (`Priority`). When the queue exceeds `Constants.MaxChatHistoryCount` (5 messages), the lowest-priority (oldest) messages are automatically popped using `RemoveValueIntent`.
2. Per-Replica Likes (`[CrdtOrSetStrategy]`): Any replica can like any active message in the queue (`like <msgId>`). Because likes are stored in an Observed-Remove Set (`ISet<string>`) keyed by `replicaId`, each replica can only like a message once, and concurrent likes across nodes merge automatically without conflicts.
3. Community Spam Moderation (`[CrdtApprovalQuorum(2)]` + `[CrdtLwwStrategy]`): Any replica can vote to flag a chat message as spam (`spam <msgId>`). Using the `ApprovalQuorum` decorator strategy, a message's `IsSpam` state will not flip to `true` until at least 2 distinct replicas have proposed marking it as spam. Once quorum is reached, the message content is hidden and displayed as `[SPAM]`.

---

## Architecture & Key Components

### 1. Defining the CRDT Models (`Models/ChatChannelState.cs` & `Models/ChatMessageState.cs`)
You define a single root `ChatChannelState` document that holds both the priority-ordered message list and a nested map of per-message CRDT states (`ChatMessageState`), decorating each property with its own CRDT strategy attribute:

```csharp
public sealed class ChatChannelState : IEquatable<ChatChannelState>
{
    public string ChannelId { get; set; } = Constants.ChatChannelDocumentId;

    [CrdtPriorityQueueStrategy(nameof(ChatItemDefinition.Priority))]
    public IList<ChatItemDefinition> Messages { get; set; } = new List<ChatItemDefinition>();

    [CrdtLwwMapStrategy]
    public IDictionary<string, ChatMessageState> MessageStates { get; set; } = new Dictionary<string, ChatMessageState>(StringComparer.Ordinal);
}

public sealed class ChatMessageState : IEquatable<ChatMessageState>
{
    public string MessageId { get; set; } = string.Empty;

    [CrdtOrSetStrategy]
    public ISet<string> LikedBy { get; set; } = new HashSet<string>(StringComparer.Ordinal);

    [CrdtApprovalQuorum(Constants.SpamQuorumCount)]
    [CrdtLwwStrategy]
    public bool IsSpam { get; set; }
}
```

Notice that `ChatChannelState` uses a domain-specific ID property (`ChannelId`) instead of a generic `Id` property. We support this custom identifier without reflection by implementing `ChatDocumentIdProvider : IDocumentIdProvider`, and we implement `ChatItemElementComparer : IElementComparer` so the priority queue uniquely identifies `ChatItemDefinition` items by their `Id`.

### 2. AOT Reflection & JSON Contexts (`Models/ChatCrdtAotContext.cs` & `Models/ChatJsonContext.cs`)
To keep the application 100% Native AOT compatible and reflection-free, we declare source-generated contexts for both CRDT property accessors (`CrdtAotContext`) and `System.Text.Json` (`JsonSerializerContext`):

```csharp
[CrdtAotType(typeof(ChatChannelState))]
[CrdtAotType(typeof(ChatMessageState))]
[CrdtAotType(typeof(ChatItemDefinition))]
[CrdtAotType(typeof(IList<ChatItemDefinition>))]
[CrdtAotType(typeof(List<ChatItemDefinition>))]
[CrdtAotType(typeof(IDictionary<string, ChatMessageState>))]
[CrdtAotType(typeof(Dictionary<string, ChatMessageState>))]
[CrdtAotType(typeof(ISet<string>))]
[CrdtAotType(typeof(HashSet<string>))]
internal sealed partial class ChatCrdtAotContext : CrdtAotContext { }
```

### 3. Custom CRDT Manager & Deep Composable Operations (`Services/ChatCrdtManager.cs`)
Your application code does not mutate `ChatChannelState` directly. Instead, `ChatCrdtManager` injects `ICrdtDocumentOrchestrator` and `ICrdtPatcher` to dispatch explicit Intents and deep composable operations within the single channel document:
- Posting Messages (`AddIntent`, `MapSetIntent`, `RemoveValueIntent`, `MapRemoveIntent`): Appends a new `ChatItemDefinition` with the current UTC timestamp as its `Priority` to `ChatChannelState.Messages`, initializes its `ChatMessageState` entry in `ChatChannelState.MessageStates`, and pops any overflow items beyond `Constants.MaxChatHistoryCount`.
- Liking a Message (`ICrdtPatcher.GenerateOperation` + `AddIntent`): Uses `patcher.GenerateOperation(channelDoc, d => d.MessageStates[msgId].LikedBy, new AddIntent(replicaId))` to target the nested OR-Set (`$.messageStates['msg-x'].likedBy`) and applies it via `orchestrator.ApplyPatchAsync`.
- Voting Spam (`ICrdtPatcher.GenerateOperation` + `SetIntent`): Uses `patcher.GenerateOperation(channelDoc, d => d.MessageStates[msgId].IsSpam, new SetIntent(true))` to target the nested ApprovalQuorum + LWW property (`$.messageStates['msg-x'].isSpam`).

### 4. Wiring into Dependency Injection (`Extensions/ChatServiceCollectionExtensions.cs`)
All custom CRDT components - including the custom `ChatDocumentIdProvider`, `ChatItemElementComparer`, polymorphic serializable type discriminators, and state observer - are registered cleanly via `IServiceCollection`:

```csharp
services.AddCrdt()
    .AddCrdtJsonTypeInfoResolver(ChatJsonContext.Default)
    .AddCrdtAotContext<ChatCrdtAotContext>()
    .AddCrdtDocumentIdProvider<ChatDocumentIdProvider>()
    .AddCrdtComparer<ChatItemElementComparer>()
    .AddCrdtSerializableType<ChatItemDefinition>(Constants.ChatItemDefinitionDiscriminator)
    .AddCrdtSerializableType<ChatMessageState>(Constants.ChatMessageStateDiscriminator)
    .AddCrdtSystemTextJson();

services.AddDistributedDocumentType<ChatChannelState>(Constants.ChatChannelDocumentType);
services.AddDistributedCrdtService<IChatCrdtManager, ChatCrdtManager>();
services.AddSingleton<IDocumentStateObserver<ChatChannelState>, ConsoleChatChannelStateObserver>();
```

---

## Running the Sample

1. Start the first node from the terminal:
   ```bash
   dotnet run --project 03-Advanced/01-CustomCrdt/Ama.Enterprise.Samples.CustomCrdt
   ```
2. Spawn a second node into the local P2P mesh by typing:
   ```text
   clone
   ```
3. Test Posting & Priority Queue Eviction:
   - On Node 1, type `post Hello from Node 1!`. A new message (e.g., `msg-a1b2`) appears across all nodes (`5/5` capacity).
   - On Node 2, type `post Another message from Node 2!`. Because the priority queue keeps only the latest `5` messages, `msg-1` (lowest priority) is automatically popped from the queue!
4. Test Per-Replica Likes:
   - On Node 1, type `like <msgId>` (using any active message ID, e.g., `like msg-2` or your newly posted message ID). Notice the likes count becomes `1 (node-8100)`.
   - On Node 1, type `like <msgId>` again. Because `LikedBy` is a set of replica IDs, the count stays `1`.
   - On Node 2, type `like <msgId>`. Both nodes synchronize and show `Likes: 2 (node-8100, node-8101)`.
5. Test Approval Quorum Spam Moderation:
   - On Node 1, type `spam <msgId>` (e.g., `spam msg-3` or a posted message ID). Notice the message is still visible because quorum requires 2 distinct nodes!
   - On Node 2, type `spam <msgId>`. Quorum (`2`) is now met! Both nodes immediately transition the message to display `[SPAM]`.

---
[⬅ Back to Advanced Overview](../README.md)
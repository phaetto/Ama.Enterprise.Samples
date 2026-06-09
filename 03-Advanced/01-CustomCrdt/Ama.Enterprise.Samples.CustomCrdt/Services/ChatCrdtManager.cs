namespace Ama.Enterprise.Samples.CustomCrdt.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ama.CRDT.Models;
using Ama.CRDT.Models.Intents;
using Ama.CRDT.Services;
using Ama.Enterprise.CRDT.Distributed.Models;
using Ama.Enterprise.CRDT.Distributed.Services;
using Ama.Enterprise.Samples.CustomCrdt;
using Ama.Enterprise.Samples.CustomCrdt.Models;

/// <summary>
/// Implementation of <see cref="IChatCrdtManager"/> that manages the bounded chat history via a CRDT Priority Queue
/// and deep nested per-message likes/spam moderation inside the same <see cref="ChatChannelState"/> document
/// using composable operations generated via <see cref="ICrdtPatcher"/>.
/// </summary>
public sealed class ChatCrdtManager : IChatCrdtManager
{
    private readonly ICrdtDocumentOrchestrator orchestrator;
    private readonly ICrdtPatcher patcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatCrdtManager"/> class.
    /// </summary>
    /// <param name="orchestrator">The distributed CRDT document orchestrator.</param>
    /// <param name="patcher">The CRDT patcher used to generate composable deep-path operations.</param>
    public ChatCrdtManager(ICrdtDocumentOrchestrator orchestrator, ICrdtPatcher patcher)
    {
        ArgumentNullException.ThrowIfNull(orchestrator);
        ArgumentNullException.ThrowIfNull(patcher);
        this.orchestrator = orchestrator;
        this.patcher = patcher;
    }

    /// <inheritdoc />
    public async Task EnsureChannelInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (await orchestrator.DocumentExistsAsync<ChatChannelState>(Constants.ChatChannelDocumentId, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        await orchestrator.CreateDocumentAsync(
            Constants.ChatChannelDocumentId,
            Constants.ChatChannelDocumentType,
            IntentDeliveryMode.Acknowledged,
            cancellationToken).ConfigureAwait(false);

        foreach (var seedItem in Constants.InitialChatItems)
        {
            await orchestrator.ApplyIntentAsync<ChatChannelState>(
                Constants.ChatChannelDocumentId,
                new AddIntent(seedItem),
                nameof(ChatChannelState.Messages),
                IntentDeliveryMode.Acknowledged,
                cancellationToken).ConfigureAwait(false);

            await orchestrator.ApplyIntentAsync<ChatChannelState>(
                Constants.ChatChannelDocumentId,
                new MapSetIntent(seedItem.Id, new ChatMessageState { MessageId = seedItem.Id }),
                nameof(ChatChannelState.MessageStates),
                IntentDeliveryMode.Acknowledged,
                cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<ChatItemView>> GetChatItemsAsync(CancellationToken cancellationToken = default)
    {
        if (!await orchestrator.DocumentExistsAsync<ChatChannelState>(Constants.ChatChannelDocumentId, cancellationToken).ConfigureAwait(false))
        {
            return [];
        }

        var channelDoc = await orchestrator.GetDocumentStateAsync<ChatChannelState>(Constants.ChatChannelDocumentId, cancellationToken).ConfigureAwait(false);
        var entries = channelDoc.Data?.Messages ?? [];
        var messageStates = channelDoc.Data?.MessageStates;

        // PriorityQueueStrategy keeps highest priority (newest) first; order ascending by Priority for chronological display
        // while trimming any transient overflow beyond MaxChatHistoryCount.
        var activeEntries = entries
            .OrderByDescending(e => e.Priority)
            .Take(Constants.MaxChatHistoryCount)
            .OrderBy(e => e.Priority)
            .ToList();

        var result = new List<ChatItemView>(activeEntries.Count);

        foreach (var definition in activeEntries)
        {
            var likedBy = new HashSet<string>(StringComparer.Ordinal);
            var isSpam = false;

            if (messageStates is not null && messageStates.TryGetValue(definition.Id, out var messageState) && messageState is not null)
            {
                isSpam = messageState.IsSpam;
                if (messageState.LikedBy is not null)
                {
                    foreach (var replica in messageState.LikedBy)
                    {
                        likedBy.Add(replica);
                    }
                }
            }

            result.Add(new ChatItemView(
                definition.Id,
                definition.Author,
                definition.Content,
                definition.Priority,
                likedBy,
                isSpam));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ChatItemDefinition> PostMessageAsync(string author, string content, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(author);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        await EnsureChannelInitializedAsync(cancellationToken).ConfigureAwait(false);

        var messageId = $"msg-{Guid.NewGuid().ToString("N")[..4]}";
        var priority = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var newEntry = new ChatItemDefinition(messageId, author, content.Trim(), priority);

        // 1. Push the new message onto the CRDT Priority Queue.
        await orchestrator.ApplyIntentAsync<ChatChannelState>(
            Constants.ChatChannelDocumentId,
            new AddIntent(newEntry),
            nameof(ChatChannelState.Messages),
            IntentDeliveryMode.Acknowledged,
            cancellationToken).ConfigureAwait(false);

        // 2. Initialize the nested ChatMessageState inside the channel's MessageStates map.
        await orchestrator.ApplyIntentAsync<ChatChannelState>(
            Constants.ChatChannelDocumentId,
            new MapSetIntent(messageId, new ChatMessageState { MessageId = messageId }),
            nameof(ChatChannelState.MessageStates),
            IntentDeliveryMode.Acknowledged,
            cancellationToken).ConfigureAwait(false);

        // 3. Pop the lowest-priority (oldest) messages if the queue exceeds MaxChatHistoryCount.
        await PopOverflowMessagesAsync(cancellationToken).ConfigureAwait(false);

        return newEntry;
    }

    /// <inheritdoc />
    public async Task LikeMessageAsync(string messageId, string replicaId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(replicaId);

        var targetEntry = await EnsureMessageExistsInChannelAsync(messageId, cancellationToken).ConfigureAwait(false);
        var channelDoc = await EnsureMessageStateEntryExistsAsync(targetEntry.Id, cancellationToken).ConfigureAwait(false);

        // Generate a composable operation targeting the nested OR-Set property inside the channel dictionary:
        // $.messageStates['msg-x'].likedBy
        var operation = patcher.GenerateOperation(
            channelDoc,
            d => d.MessageStates[targetEntry.Id].LikedBy,
            new AddIntent(replicaId));

        await orchestrator.ApplyPatchAsync<ChatChannelState>(
            Constants.ChatChannelDocumentId,
            new CrdtPatch([operation]),
            IntentDeliveryMode.Acknowledged,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task VoteSpamAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        var targetEntry = await EnsureMessageExistsInChannelAsync(messageId, cancellationToken).ConfigureAwait(false);
        var channelDoc = await EnsureMessageStateEntryExistsAsync(targetEntry.Id, cancellationToken).ConfigureAwait(false);

        // Generate a composable operation targeting the nested ApprovalQuorum + LWW property inside the channel dictionary:
        // $.messageStates['msg-x'].isSpam
        var operation = patcher.GenerateOperation(
            channelDoc,
            d => d.MessageStates[targetEntry.Id].IsSpam,
            new SetIntent(true));

        await orchestrator.ApplyPatchAsync<ChatChannelState>(
            Constants.ChatChannelDocumentId,
            new CrdtPatch([operation]),
            IntentDeliveryMode.Acknowledged,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task PopOverflowMessagesAsync(CancellationToken cancellationToken)
    {
        var currentMessages = await GetChannelMessagesAsync(cancellationToken).ConfigureAwait(false);
        if (currentMessages.Count <= Constants.MaxChatHistoryCount)
        {
            return;
        }

        // PriorityQueueStrategy sorts descending by Priority (index 0 = highest/newest, tail = lowest/oldest).
        var messagesToPop = currentMessages
            .OrderByDescending(m => m.Priority)
            .Skip(Constants.MaxChatHistoryCount)
            .ToList();

        foreach (var oldMessage in messagesToPop)
        {
            await orchestrator.ApplyIntentAsync<ChatChannelState>(
                Constants.ChatChannelDocumentId,
                new RemoveValueIntent(oldMessage),
                nameof(ChatChannelState.Messages),
                IntentDeliveryMode.Acknowledged,
                cancellationToken).ConfigureAwait(false);

            await orchestrator.ApplyIntentAsync<ChatChannelState>(
                Constants.ChatChannelDocumentId,
                new MapRemoveIntent(oldMessage.Id),
                nameof(ChatChannelState.MessageStates),
                IntentDeliveryMode.Acknowledged,
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask<IReadOnlyList<ChatItemDefinition>> GetChannelMessagesAsync(CancellationToken cancellationToken)
    {
        if (!await orchestrator.DocumentExistsAsync<ChatChannelState>(Constants.ChatChannelDocumentId, cancellationToken).ConfigureAwait(false))
        {
            return [];
        }

        var channelDoc = await orchestrator.GetDocumentStateAsync<ChatChannelState>(Constants.ChatChannelDocumentId, cancellationToken).ConfigureAwait(false);
        if (channelDoc.Data?.Messages is null)
        {
            return [];
        }

        return channelDoc.Data.Messages.ToList();
    }

    private async Task<ChatItemDefinition> EnsureMessageExistsInChannelAsync(string messageId, CancellationToken cancellationToken)
    {
        var messages = await GetChannelMessagesAsync(cancellationToken).ConfigureAwait(false);
        foreach (var item in messages)
        {
            if (string.Equals(item.Id, messageId, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }

        throw new ArgumentException($"Chat message '{messageId}' is not in the active priority queue.", nameof(messageId));
    }

    private async Task<CrdtDocument<ChatChannelState>> EnsureMessageStateEntryExistsAsync(string messageId, CancellationToken cancellationToken)
    {
        var channelDoc = await orchestrator.GetDocumentStateAsync<ChatChannelState>(Constants.ChatChannelDocumentId, cancellationToken).ConfigureAwait(false);
        if (channelDoc.Data?.MessageStates is null || !channelDoc.Data.MessageStates.ContainsKey(messageId))
        {
            await orchestrator.ApplyIntentAsync<ChatChannelState>(
                Constants.ChatChannelDocumentId,
                new MapSetIntent(messageId, new ChatMessageState { MessageId = messageId }),
                nameof(ChatChannelState.MessageStates),
                IntentDeliveryMode.Acknowledged,
                cancellationToken).ConfigureAwait(false);

            channelDoc = await orchestrator.GetDocumentStateAsync<ChatChannelState>(Constants.ChatChannelDocumentId, cancellationToken).ConfigureAwait(false);
        }

        return channelDoc;
    }
}
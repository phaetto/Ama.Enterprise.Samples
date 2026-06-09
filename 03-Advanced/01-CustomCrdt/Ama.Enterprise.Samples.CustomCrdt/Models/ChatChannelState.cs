namespace Ama.Enterprise.Samples.CustomCrdt.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using Ama.CRDT.Attributes.Strategies;
using Ama.Enterprise.Samples.CustomCrdt;

/// <summary>
/// Represents the replicated CRDT state for the chat channel history using a priority queue.
/// </summary>
public sealed class ChatChannelState : IEquatable<ChatChannelState>
{
    /// <summary>
    /// Gets or sets the unique channel identifier used by <see cref="Services.ChatDocumentIdProvider"/>.
    /// </summary>
    public string ChannelId { get; set; } = Constants.ChatChannelDocumentId;

    /// <summary>
    /// Gets or sets the priority-ordered list of chat messages in the channel.
    /// Uses <see cref="CrdtPriorityQueueStrategyAttribute"/> ordered by <see cref="ChatItemDefinition.Priority"/>.
    /// </summary>
    [CrdtPriorityQueueStrategy(nameof(ChatItemDefinition.Priority))]
    public IList<ChatItemDefinition> Messages { get; set; } = new List<ChatItemDefinition>();

    /// <summary>
    /// Gets or sets the nested map of per-message CRDT states (likes and spam quorum moderation) keyed by message ID.
    /// Uses <see cref="CrdtLwwMapStrategyAttribute"/> and supports deep composable CRDT operations.
    /// </summary>
    [CrdtLwwMapStrategy]
    public IDictionary<string, ChatMessageState> MessageStates { get; set; } = new Dictionary<string, ChatMessageState>(StringComparer.Ordinal);

    /// <inheritdoc />
    public bool Equals(ChatChannelState? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (!string.Equals(ChannelId, other.ChannelId, StringComparison.Ordinal))
        {
            return false;
        }

        if (!ReferenceEquals(Messages, other.Messages))
        {
            if (Messages is null || other.Messages is null || !Messages.SequenceEqual(other.Messages))
            {
                return false;
            }
        }

        if (ReferenceEquals(MessageStates, other.MessageStates))
        {
            return true;
        }

        if (MessageStates is null || other.MessageStates is null || MessageStates.Count != other.MessageStates.Count)
        {
            return false;
        }

        foreach (var kvp in MessageStates)
        {
            if (!other.MessageStates.TryGetValue(kvp.Key, out var otherState) || !Equals(kvp.Value, otherState))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ChatChannelState);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ChannelId, StringComparer.Ordinal);

        if (Messages is not null)
        {
            foreach (var message in Messages)
            {
                hash.Add(message);
            }
        }

        if (MessageStates is not null)
        {
            foreach (var kvp in MessageStates.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                hash.Add(kvp.Key, StringComparer.Ordinal);
                hash.Add(kvp.Value);
            }
        }

        return hash.ToHashCode();
    }
}
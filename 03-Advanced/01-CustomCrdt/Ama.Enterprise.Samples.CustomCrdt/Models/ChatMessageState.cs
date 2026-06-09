namespace Ama.Enterprise.Samples.CustomCrdt.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using Ama.CRDT.Attributes.Decorators;
using Ama.CRDT.Attributes.Strategies;
using Ama.Enterprise.Samples.CustomCrdt;

/// <summary>
/// Represents the replicated CRDT state for an individual chat message.
/// </summary>
public sealed class ChatMessageState : IEquatable<ChatMessageState>
{
    /// <summary>
    /// Gets or sets the unique message identifier used by <see cref="Services.ChatDocumentIdProvider"/>.
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the set of replica IDs that have liked this message.
    /// Uses an OR-Set (Observed-Remove Set) so each replica is tracked uniquely once.
    /// </summary>
    [CrdtOrSetStrategy]
    public ISet<string> LikedBy { get; set; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets a value indicating whether this message is marked as spam.
    /// Decorated with <see cref="CrdtApprovalQuorumAttribute"/> so it only transitions to <c>true</c>
    /// once at least <see cref="Constants.SpamQuorumCount"/> distinct replicas vote for it.
    /// </summary>
    [CrdtApprovalQuorum(Constants.SpamQuorumCount)]
    [CrdtLwwStrategy]
    public bool IsSpam { get; set; }

    /// <inheritdoc />
    public bool Equals(ChatMessageState? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (!string.Equals(MessageId, other.MessageId, StringComparison.Ordinal) ||
            IsSpam != other.IsSpam)
        {
            return false;
        }

        if (ReferenceEquals(LikedBy, other.LikedBy))
        {
            return true;
        }

        if (LikedBy is null || other.LikedBy is null)
        {
            return false;
        }

        return LikedBy.SetEquals(other.LikedBy);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ChatMessageState);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(MessageId, StringComparer.Ordinal);
        hash.Add(IsSpam);

        if (LikedBy is not null)
        {
            foreach (var item in LikedBy.OrderBy(x => x, StringComparer.Ordinal))
            {
                hash.Add(item, StringComparer.Ordinal);
            }
        }

        return hash.ToHashCode();
    }
}
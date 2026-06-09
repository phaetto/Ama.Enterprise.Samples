namespace Ama.Enterprise.Samples.CustomCrdt.Models;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a read-only view of a chat message combined with its live CRDT state.
/// </summary>
public readonly record struct ChatItemView : IEquatable<ChatItemView>
{
    /// <summary>
    /// Gets the unique identifier of the chat item.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the author of the chat item.
    /// </summary>
    public string Author { get; }

    /// <summary>
    /// Gets the text content of the chat item.
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Gets the priority value (timestamp order) of the chat item in the priority queue.
    /// </summary>
    public long Priority { get; }

    /// <summary>
    /// Gets the set of replica identifiers that have liked this message.
    /// </summary>
    public IReadOnlySet<string> LikedByReplicas { get; }

    /// <summary>
    /// Gets a value indicating whether the message has reached the approval quorum to be marked as spam.
    /// </summary>
    public bool IsSpam { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatItemView"/> struct.
    /// </summary>
    /// <param name="id">The message identifier.</param>
    /// <param name="author">The message author.</param>
    /// <param name="content">The message content.</param>
    /// <param name="priority">The priority score of the message.</param>
    /// <param name="likedByReplicas">The set of replicas that liked the message.</param>
    /// <param name="isSpam">A value indicating whether the message is marked as spam.</param>
    public ChatItemView(
        string id,
        string author,
        string content,
        long priority,
        IReadOnlySet<string> likedByReplicas,
        bool isSpam)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(author);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentNullException.ThrowIfNull(likedByReplicas);

        Id = id;
        Author = author;
        Content = content;
        Priority = priority;
        LikedByReplicas = likedByReplicas;
        IsSpam = isSpam;
    }

    /// <inheritdoc />
    public bool Equals(ChatItemView other)
    {
        if (!string.Equals(Id, other.Id, StringComparison.Ordinal) ||
            !string.Equals(Author, other.Author, StringComparison.Ordinal) ||
            !string.Equals(Content, other.Content, StringComparison.Ordinal) ||
            Priority != other.Priority ||
            IsSpam != other.IsSpam)
        {
            return false;
        }

        if (ReferenceEquals(LikedByReplicas, other.LikedByReplicas))
        {
            return true;
        }

        if (LikedByReplicas is null || other.LikedByReplicas is null)
        {
            return false;
        }

        return LikedByReplicas.SetEquals(other.LikedByReplicas);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Id, StringComparer.Ordinal);
        hash.Add(Author, StringComparer.Ordinal);
        hash.Add(Content, StringComparer.Ordinal);
        hash.Add(Priority);
        hash.Add(IsSpam);

        if (LikedByReplicas is not null)
        {
            foreach (var replica in LikedByReplicas.OrderBy(r => r, StringComparer.Ordinal))
            {
                hash.Add(replica, StringComparer.Ordinal);
            }
        }

        return hash.ToHashCode();
    }
}
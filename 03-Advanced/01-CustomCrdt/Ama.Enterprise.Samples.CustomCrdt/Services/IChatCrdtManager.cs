namespace Ama.Enterprise.Samples.CustomCrdt.Services;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ama.Enterprise.Samples.CustomCrdt;
using Ama.Enterprise.Samples.CustomCrdt.Models;

/// <summary>
/// Defines the manager service for interacting with the distributed Chat CRDT channel priority queue and message states.
/// </summary>
public interface IChatCrdtManager
{
    /// <summary>
    /// Ensures the shared chat channel priority queue is initialized and seeded if not already present.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EnsureChannelInitializedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the active chat messages in the priority queue combined with their live replicated likes and spam state.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A read-only list of chat item views ordered chronologically.</returns>
    ValueTask<IReadOnlyList<ChatItemView>> GetChatItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Posts a new chat message to the priority queue across all replicas and pops the oldest entries
    /// if the history exceeds <see cref="Constants.MaxChatHistoryCount"/>.
    /// </summary>
    /// <param name="author">The replica or user posting the message.</param>
    /// <param name="content">The text content of the message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created <see cref="ChatItemDefinition"/>.</returns>
    Task<ChatItemDefinition> PostMessageAsync(string author, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Likes a chat message on behalf of a specific replica (idempotent per replica).
    /// </summary>
    /// <param name="messageId">The identifier of the message to like.</param>
    /// <param name="replicaId">The identifier of the replica liking the message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LikeMessageAsync(string messageId, string replicaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits a quorum vote from the current replica to mark a chat message as spam.
    /// Once <see cref="Constants.SpamQuorumCount"/> distinct replicas vote, the message is marked as spam.
    /// </summary>
    /// <param name="messageId">The identifier of the message to vote as spam.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task VoteSpamAsync(string messageId, CancellationToken cancellationToken = default);
}
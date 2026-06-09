namespace Ama.Enterprise.Samples.CustomCrdt;

using System.Collections.Generic;
using Ama.Enterprise.Samples.CustomCrdt.Models;

/// <summary>
/// Global constants for the custom Chat CRDT showcase.
/// </summary>
public static class Constants
{
    /// <summary>
    /// The unique document identifier for the shared chat channel priority queue.
    /// </summary>
    public const string ChatChannelDocumentId = "general-chat-channel";

    /// <summary>
    /// The registered distributed document type name for the chat channel history.
    /// </summary>
    public const string ChatChannelDocumentType = "chat-channel";

    /// <summary>
    /// The discriminator of a complex deeper type for serialization AOT marking.
    /// </summary>
    public const string ChatItemDefinitionDiscriminator = "chat-item";

    /// <summary>
    /// The discriminator of a chat message state item for serialization AOT marking.
    /// </summary>
    public const string ChatMessageStateDiscriminator = "chat-message-state";

    /// <summary>
    /// The number of distinct replica votes required to mark a message as spam.
    /// </summary>
    public const int SpamQuorumCount = 2;

    /// <summary>
    /// The maximum number of recent chat entries retained in the priority queue.
    /// Older entries with lower priority are automatically popped when new messages are posted.
    /// </summary>
    public const int MaxChatHistoryCount = 5;

    /// <summary>
    /// The initial seed chat items added to the showcase channel when bootstrapped.
    /// </summary>
    public static readonly IReadOnlyList<ChatItemDefinition> InitialChatItems =
    [
        new ChatItemDefinition("msg-1", "Alice", "Welcome to the distributed P2P chat showcase!", 1),
        new ChatItemDefinition("msg-2", "Bob", "Use 'post <text>' to broadcast a message. Only the latest 5 are kept!", 2),
        new ChatItemDefinition("msg-3", "Mallory", "FREE CRYPTO! Click this super suspicious link now!!!", 3),
        new ChatItemDefinition("msg-4", "Charlie", "Let's test ApprovalQuorum by flagging msg-3 as spam, or like any message!", 4)
    ];
}
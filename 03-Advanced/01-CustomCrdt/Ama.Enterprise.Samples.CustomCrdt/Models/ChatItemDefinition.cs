namespace Ama.Enterprise.Samples.CustomCrdt.Models;

using System;

/// <summary>
/// Represents a chat message entry stored inside the channel's CRDT priority queue.
/// </summary>
/// <param name="Id">The unique message identifier.</param>
/// <param name="Author">The author (replica ID) of the message.</param>
/// <param name="Content">The text content of the message.</param>
/// <param name="Priority">The priority score (UTC timestamp milliseconds) used to order and bound the queue.</param>
public readonly record struct ChatItemDefinition(
    string Id,
    string Author,
    string Content,
    long Priority) : IEquatable<ChatItemDefinition>;
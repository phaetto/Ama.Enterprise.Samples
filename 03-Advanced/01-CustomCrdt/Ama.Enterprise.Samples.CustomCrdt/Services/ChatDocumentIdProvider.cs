namespace Ama.Enterprise.Samples.CustomCrdt.Services;

using System;
using Ama.CRDT.Services.Providers;
using Ama.Enterprise.Samples.CustomCrdt.Models;

/// <summary>
/// Custom <see cref="IDocumentIdProvider"/> that extracts, sets, and instantiates the custom channel identifier
/// (<see cref="ChatChannelState.ChannelId"/>) without relying on a default <c>Id</c> property.
/// </summary>
public sealed class ChatDocumentIdProvider : IDocumentIdProvider
{
    /// <inheritdoc />
    public IComparable GetDocumentId<T>(T? obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        return obj switch
        {
            ChatChannelState channel when !string.IsNullOrWhiteSpace(channel.ChannelId) => channel.ChannelId,
            _ => throw new InvalidOperationException($"Unable to extract a valid custom document identifier from type '{typeof(T).Name}'.")
        };
    }

    /// <inheritdoc />
    public void SetDocumentId<T>(T obj, IComparable id)
    {
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentNullException.ThrowIfNull(id);

        var stringId = id.ToString();
        ArgumentException.ThrowIfNullOrWhiteSpace(stringId);

        switch (obj)
        {
            case ChatChannelState channel:
                channel.ChannelId = stringId;
                break;
            default:
                throw new InvalidOperationException($"Cannot set custom document identifier on unsupported type '{typeof(T).Name}'.");
        }
    }

    /// <inheritdoc />
    public T CreateDocumentWithId<T>(IComparable id)
    {
        ArgumentNullException.ThrowIfNull(id);

        var stringId = id.ToString();
        ArgumentException.ThrowIfNullOrWhiteSpace(stringId);

        if (typeof(T) == typeof(ChatChannelState))
        {
            var channel = new ChatChannelState
            {
                ChannelId = stringId
            };
            return (T)(object)channel;
        }

        throw new InvalidOperationException($"Cannot instantiate unsupported document type '{typeof(T).Name}' with custom ID '{stringId}'.");
    }
}
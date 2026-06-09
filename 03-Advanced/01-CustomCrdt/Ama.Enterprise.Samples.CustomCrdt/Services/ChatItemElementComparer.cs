namespace Ama.Enterprise.Samples.CustomCrdt.Services;

using System;
using System.Diagnostics.CodeAnalysis;
using Ama.CRDT.Services.Providers;
using Ama.Enterprise.Samples.CustomCrdt.Models;

/// <summary>
/// Element comparer for <see cref="ChatItemDefinition"/> that identifies unique chat entries by <see cref="ChatItemDefinition.Id"/>
/// within the CRDT priority queue.
/// </summary>
public sealed class ChatItemElementComparer : IElementComparer
{
    /// <inheritdoc />
    public bool CanCompare([DisallowNull] Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type == typeof(ChatItemDefinition);
    }

    /// <inheritdoc />
    public new bool Equals(object? x, object? y)
    {
        if (x is ChatItemDefinition itemX && y is ChatItemDefinition itemY)
        {
            return string.Equals(itemX.Id, itemY.Id, StringComparison.OrdinalIgnoreCase);
        }

        return object.Equals(x, y);
    }

    /// <inheritdoc />
    public int GetHashCode(object obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        if (obj is ChatItemDefinition item && item.Id is not null)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(item.Id);
        }

        return obj.GetHashCode();
    }
}
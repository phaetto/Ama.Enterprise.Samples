namespace Ama.Enterprise.Samples.Sharding.Services;

using System;
using Ama.CRDT.Models;
using Ama.CRDT.Services.Providers;
using Ama.Enterprise.Samples.Sharding.Models;

/// <summary>
/// Custom <see cref="IShardIdProvider"/> that maps each <see cref="TenantFeatureFlagState"/> document
/// to its own dedicated shard ID (<c>tenant-shard-{documentId}</c>) so the consistent hash ring
/// distributes and rebalances tenant documents evenly across active cluster nodes.
/// </summary>
public sealed class TenantShardIdProvider : IShardIdProvider
{
    /// <inheritdoc />
    public string GetShardId<T>(CrdtDocument<T> document) where T : class
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.Data is TenantFeatureFlagState state && !string.IsNullOrWhiteSpace(state.Id))
        {
            return $"{Constants.TenantShardPrefix}{state.Id}";
        }

        return "default";
    }

    /// <inheritdoc />
    public string GetShardId<T>(IComparable documentId) where T : class
    {
        ArgumentNullException.ThrowIfNull(documentId);
        return GetShardId(documentId, typeof(T));
    }

    /// <inheritdoc />
    public string GetShardId(IComparable documentId, Type documentType)
    {
        ArgumentNullException.ThrowIfNull(documentId);
        ArgumentNullException.ThrowIfNull(documentType);

        if (documentType == typeof(TenantFeatureFlagState))
        {
            return $"{Constants.TenantShardPrefix}{documentId}";
        }

        return "default";
    }
}
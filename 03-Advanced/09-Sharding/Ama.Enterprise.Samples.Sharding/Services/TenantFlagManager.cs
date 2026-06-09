namespace Ama.Enterprise.Samples.Sharding.Services;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ama.CRDT.Models.Intents;
using Ama.CRDT.Services.Providers;
using Ama.Enterprise.CRDT.Distributed.Models;
using Ama.Enterprise.CRDT.Distributed.Services;
using Ama.Enterprise.Samples.Sharding.Models;

/// <summary>
/// Implementation of <see cref="ITenantFlagManager"/> that delegates operations to <see cref="ICrdtDocumentOrchestrator"/>
/// and inspects consistent hash ring placement via <see cref="IShardDistributionManager"/>.
/// </summary>
public sealed class TenantFlagManager : ITenantFlagManager
{
    private static readonly IReadOnlyDictionary<string, bool> EmptyFlags =
        new ReadOnlyDictionary<string, bool>(new Dictionary<string, bool>(StringComparer.Ordinal));

    private readonly ICrdtDocumentOrchestrator orchestrator;
    private readonly IShardDistributionManager shardDistributionManager;
    private readonly IShardIdProvider shardIdProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantFlagManager"/> class.
    /// </summary>
    /// <param name="orchestrator">The multi-document CRDT orchestrator.</param>
    /// <param name="shardDistributionManager">The consistent hash ring shard distribution manager.</param>
    /// <param name="shardIdProvider">The shard ID provider.</param>
    public TenantFlagManager(
        ICrdtDocumentOrchestrator orchestrator,
        IShardDistributionManager shardDistributionManager,
        IShardIdProvider shardIdProvider)
    {
        ArgumentNullException.ThrowIfNull(orchestrator);
        ArgumentNullException.ThrowIfNull(shardDistributionManager);
        ArgumentNullException.ThrowIfNull(shardIdProvider);

        this.orchestrator = orchestrator;
        this.shardDistributionManager = shardDistributionManager;
        this.shardIdProvider = shardIdProvider;
    }

    /// <inheritdoc />
    public async Task CreateTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        await orchestrator.CreateDocumentAsync(
            tenantId,
            Constants.TenantFlagsDocType,
            IntentDeliveryMode.Acknowledged,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        await orchestrator.DeleteDocumentAsync(
            tenantId,
            IntentDeliveryMode.Acknowledged,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SetFlagAsync(string tenantId, string flagName, bool isEnabled, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(flagName);

        await orchestrator.CreateDocumentAsync(
            tenantId,
            Constants.TenantFlagsDocType,
            IntentDeliveryMode.Acknowledged,
            cancellationToken).ConfigureAwait(false);

        await orchestrator.ApplyIntentAsync<TenantFeatureFlagState>(
            tenantId,
            new MapSetIntent(flagName, isEnabled),
            nameof(TenantFeatureFlagState.Flags),
            IntentDeliveryMode.Acknowledged,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemoveFlagAsync(string tenantId, string flagName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(flagName);

        await orchestrator.ApplyIntentAsync<TenantFeatureFlagState>(
            tenantId,
            new MapRemoveIntent(flagName),
            nameof(TenantFeatureFlagState.Flags),
            IntentDeliveryMode.Acknowledged,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, bool>> GetTenantFlagsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var document = await orchestrator.GetDocumentStateAsync<TenantFeatureFlagState>(tenantId, cancellationToken).ConfigureAwait(false);
        if (document.Data?.Flags is null || document.Data.Flags.Count == 0)
        {
            return EmptyFlags;
        }

        return new ReadOnlyDictionary<string, bool>(new Dictionary<string, bool>(document.Data.Flags, StringComparer.Ordinal));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TenantShardOverviewDto>> GetClusterShardOverviewAsync(CancellationToken cancellationToken = default)
    {
        var registryState = await orchestrator.Registry.GetDocumentAsync(cancellationToken).ConfigureAwait(false);
        var activeDocs = registryState.Data?.Documents;
        if (activeDocs is null || activeDocs.Count == 0)
        {
            return Array.Empty<TenantShardOverviewDto>();
        }

        var results = new List<TenantShardOverviewDto>(activeDocs.Count);
        foreach (var kvp in activeDocs.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            if (!string.Equals(kvp.Value.TypeAlias, Constants.TenantFlagsDocType, StringComparison.Ordinal))
            {
                continue;
            }

            var docId = kvp.Key;
            var shardId = shardIdProvider.GetShardId<TenantFeatureFlagState>(docId);
            var isLocal = await shardDistributionManager.IsShardLocalAsync(shardId, cancellationToken).ConfigureAwait(false);
            var assignedReplicas = await shardDistributionManager.GetTargetNodesForShardAsync(shardId, cancellationToken).ConfigureAwait(false);

            var localDoc = orchestrator.TryGetLocalDocument<TenantFeatureFlagState>(docId);
            IReadOnlyDictionary<string, bool> localFlags = EmptyFlags;
            long wholeDocBytes = 0;
            long shardDvvBytes = 0;

            if (localDoc is not null)
            {
                var state = await localDoc.GetDocumentAsync(cancellationToken).ConfigureAwait(false);
                if (state.Data?.Flags is not null && state.Data.Flags.Count > 0)
                {
                    localFlags = new ReadOnlyDictionary<string, bool>(new Dictionary<string, bool>(state.Data.Flags, StringComparer.Ordinal));
                }

                var footprint = await localDoc.EstimateMemoryFootprintAsync(cancellationToken).ConfigureAwait(false);
                wholeDocBytes = footprint.WholeDocumentBytes;
                shardDvvBytes = footprint.ShardVersionVectorBytes;
            }

            results.Add(new TenantShardOverviewDto(
                docId,
                shardId,
                isLocal,
                assignedReplicas,
                localFlags,
                wholeDocBytes,
                shardDvvBytes));
        }

        return results;
    }
}
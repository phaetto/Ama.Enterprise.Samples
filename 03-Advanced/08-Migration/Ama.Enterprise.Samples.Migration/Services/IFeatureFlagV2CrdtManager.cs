namespace Ama.Enterprise.Samples.Migration.Services;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ama.Enterprise.CRDT.Distributed.Models;
using Ama.Enterprise.Samples.Migration.Models;

/// <summary>
/// Defines the manager for reading and mutating V2 feature flag rules via the CRDT orchestrator.
/// </summary>
public interface IFeatureFlagV2CrdtManager
{
    /// <summary>
    /// Retrieves all V2 feature flag rules currently stored in the replica's state.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A read-only dictionary of V2 feature flag rules keyed by flag key.</returns>
    ValueTask<IReadOnlyDictionary<string, FeatureFlagRuleV2>> GetRulesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a V2 feature flag rule with rollout percentage and state.
    /// </summary>
    /// <param name="key">The unique key of the feature flag.</param>
    /// <param name="rolloutPercentage">The rollout percentage between 0 and 100.</param>
    /// <param name="owner">Optional owner or author identifier.</param>
    /// <param name="deliveryMode">The intent delivery mode.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetRuleAsync(
        string key,
        int rolloutPercentage,
        string? owner = null,
        IntentDeliveryMode deliveryMode = IntentDeliveryMode.Acknowledged,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a V2 feature flag rule from the cluster state.
    /// </summary>
    /// <param name="key">The unique key of the feature flag to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveRuleAsync(string key, CancellationToken cancellationToken = default);
}
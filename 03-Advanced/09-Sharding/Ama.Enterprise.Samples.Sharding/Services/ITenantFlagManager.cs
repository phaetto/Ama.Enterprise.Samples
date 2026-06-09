namespace Ama.Enterprise.Samples.Sharding.Services;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ama.Enterprise.Samples.Sharding.Models;

/// <summary>
/// Service interface for creating, mutating, and inspecting sharded tenant feature flag documents across the cluster.
/// </summary>
public interface ITenantFlagManager
{
    /// <summary>
    /// Creates a new sharded tenant feature flag document in the cluster registry.
    /// </summary>
    /// <param name="tenantId">The unique tenant document identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateTenantAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tombstones and deletes a sharded tenant document from the cluster.
    /// </summary>
    /// <param name="tenantId">The unique tenant document identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteTenantAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets or updates a feature flag on a tenant document. If the shard is hosted on a remote node,
    /// the orchestrator transparently routes the intent to the owning replica(s) via P2P RPC.
    /// </summary>
    /// <param name="tenantId">The unique tenant document identifier.</param>
    /// <param name="flagName">The name of the feature flag.</param>
    /// <param name="isEnabled">Whether the feature flag is enabled.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetFlagAsync(string tenantId, string flagName, bool isEnabled, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a feature flag from a tenant document.
    /// </summary>
    /// <param name="tenantId">The unique tenant document identifier.</param>
    /// <param name="flagName">The name of the feature flag to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveFlagAsync(string tenantId, string flagName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the current feature flags for a tenant document, transparently fetching from a remote shard owner via P2P RPC if the shard is not hosted locally.
    /// </summary>
    /// <param name="tenantId">The unique tenant document identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A read-only dictionary of feature flags for the requested tenant.</returns>
    Task<IReadOnlyDictionary<string, bool>> GetTenantFlagsAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a cluster-wide shard placement overview for all registered tenant documents, indicating which shards are hosted locally versus remotely.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A read-only list of <see cref="TenantShardOverviewDto"/> items.</returns>
    Task<IReadOnlyList<TenantShardOverviewDto>> GetClusterShardOverviewAsync(CancellationToken cancellationToken = default);
}
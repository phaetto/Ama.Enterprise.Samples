namespace Ama.Enterprise.Samples.Sharding.Models;

using System.Collections.Generic;

/// <summary>
/// Data transfer object describing a tenant document's shard placement, hosting status, and memory footprint.
/// </summary>
/// <param name="DocumentId">The unique tenant document identifier.</param>
/// <param name="ShardId">The computed shard identifier.</param>
/// <param name="IsHostedLocally">Whether the local replica is an assigned owner for this shard on the hash ring.</param>
/// <param name="AssignedReplicas">The list of replica IDs responsible for hosting this shard.</param>
/// <param name="LocalFlags">The locally hosted feature flags if hosted on this node; otherwise empty.</param>
/// <param name="WholeDocumentBytes">The estimated serialized memory footprint of the local document in bytes.</param>
/// <param name="ShardDvvBytes">The estimated serialized memory footprint of the shard's Dotted Version Vector in bytes.</param>
public readonly record struct TenantShardOverviewDto(
    string DocumentId,
    string ShardId,
    bool IsHostedLocally,
    IReadOnlyList<string> AssignedReplicas,
    IReadOnlyDictionary<string, bool> LocalFlags,
    long WholeDocumentBytes,
    long ShardDvvBytes);
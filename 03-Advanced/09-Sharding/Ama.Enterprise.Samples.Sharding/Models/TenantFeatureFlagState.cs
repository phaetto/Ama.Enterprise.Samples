namespace Ama.Enterprise.Samples.Sharding.Models;

using System;
using System.Collections.Generic;
using Ama.CRDT.Attributes.Strategies;

/// <summary>
/// Represents the CRDT state for a specific tenant's feature flags document,
/// partitioned onto its own logical shard on the cluster's consistent hash ring.
/// </summary>
public sealed class TenantFeatureFlagState : IEquatable<TenantFeatureFlagState>
{
    /// <summary>
    /// Gets or sets the unique document ID (e.g., the tenant identifier).
    /// </summary>
    [CrdtLwwStrategy]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the dictionary of feature flags and their enabled states for this tenant.
    /// </summary>
    [CrdtOrMapStrategy]
    public IDictionary<string, bool> Flags { get; set; } = new Dictionary<string, bool>(StringComparer.Ordinal);

    /// <inheritdoc />
    public bool Equals(TenantFeatureFlagState? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (!string.Equals(Id, other.Id, StringComparison.Ordinal) || Flags.Count != other.Flags.Count)
        {
            return false;
        }

        foreach (var kvp in Flags)
        {
            if (!other.Flags.TryGetValue(kvp.Key, out var otherVal) || kvp.Value != otherVal)
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as TenantFeatureFlagState);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Id, Flags.Count);
}
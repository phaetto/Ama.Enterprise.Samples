namespace Ama.Enterprise.Samples.Migration.Models;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ama.Enterprise.P2p.Models.Core;

/// <summary>
/// Represents a V2 enterprise feature flag rule that introduces a breaking schema change
/// from V1 (replacing boolean IsEnabled with string State and RolloutPercentage, and flattening metadata).
/// </summary>
public sealed class FeatureFlagRuleV2 : IEquatable<FeatureFlagRuleV2>, IExtensibleDistributedPayload
{
    /// <summary>
    /// Gets or sets the unique key of the feature flag.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rollout state (e.g., "Active", "Inactive", "Canary").
    /// </summary>
    public string State { get; set; } = "Inactive";

    /// <summary>
    /// Gets or sets the percentage of traffic receiving this feature (0 to 100).
    /// </summary>
    public int RolloutPercentage { get; set; }

    /// <summary>
    /// Gets or sets the flattened owner/author identifier.
    /// </summary>
    public string? Owner { get; set; }

    /// <summary>
    /// Gets or sets the schema version of this model.
    /// </summary>
    public int SchemaVersion { get; set; } = 2;

    /// <summary>
    /// Gets or sets the last updated timestamp in Unix milliseconds.
    /// </summary>
    public long UpdatedAtUnixMs { get; set; }

    /// <inheritdoc />
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? JsonExtensionData { get; set; }

    /// <inheritdoc />
    [JsonIgnore]
    public IList<ReadOnlyMemory<byte>>? BinaryExtensionData { get; set; }

    /// <inheritdoc />
    public bool Equals(FeatureFlagRuleV2? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return string.Equals(Key, other.Key, StringComparison.Ordinal) &&
               string.Equals(State, other.State, StringComparison.Ordinal) &&
               RolloutPercentage == other.RolloutPercentage &&
               string.Equals(Owner, other.Owner, StringComparison.Ordinal) &&
               SchemaVersion == other.SchemaVersion &&
               UpdatedAtUnixMs == other.UpdatedAtUnixMs;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as FeatureFlagRuleV2);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Key, State, RolloutPercentage, Owner, SchemaVersion, UpdatedAtUnixMs);
}
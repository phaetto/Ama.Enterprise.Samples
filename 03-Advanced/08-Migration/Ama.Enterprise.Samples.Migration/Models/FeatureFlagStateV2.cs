namespace Ama.Enterprise.Samples.Migration.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ama.CRDT.Attributes.Strategies;
using Ama.Enterprise.P2p.Models.Core;

/// <summary>
/// The root CRDT document state for V2 feature flags, breaking the V1 <c>Flags</c> dictionary contract
/// by using a <c>Rules</c> dictionary with <see cref="FeatureFlagRuleV2"/> entries.
/// </summary>
public sealed class FeatureFlagStateV2 : IEquatable<FeatureFlagStateV2>, IExtensibleDistributedPayload
{
    /// <summary>
    /// Gets or sets the document identifier.
    /// </summary>
    public string Id { get; set; } = Ama.Enterprise.FeatureFlags.Constants.GlobalDocumentId;

    /// <summary>
    /// Gets or sets the map of V2 feature flag rules keyed by flag key.
    /// </summary>
    [CrdtOrMapStrategy]
    public IDictionary<string, FeatureFlagRuleV2> Rules { get; set; } = new Dictionary<string, FeatureFlagRuleV2>(StringComparer.Ordinal);

    /// <inheritdoc />
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? JsonExtensionData { get; set; }

    /// <inheritdoc />
    [JsonIgnore]
    public IList<ReadOnlyMemory<byte>>? BinaryExtensionData { get; set; }

    /// <inheritdoc />
    public bool Equals(FeatureFlagStateV2? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        if (!string.Equals(Id, other.Id, StringComparison.Ordinal)) return false;
        if (Rules.Count != other.Rules.Count) return false;

        foreach (var kvp in Rules)
        {
            if (!other.Rules.TryGetValue(kvp.Key, out var otherRule)) return false;
            if (!kvp.Value.Equals(otherRule)) return false;
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as FeatureFlagStateV2);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Id);

        foreach (var kvp in Rules.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            hash.Add(kvp.Key);
            hash.Add(kvp.Value);
        }

        return hash.ToHashCode();
    }
}
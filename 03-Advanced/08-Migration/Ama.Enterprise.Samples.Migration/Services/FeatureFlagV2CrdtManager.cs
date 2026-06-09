namespace Ama.Enterprise.Samples.Migration.Services;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Ama.CRDT.Models.Intents;
using Ama.Enterprise.CRDT.Distributed.Models;
using Ama.Enterprise.CRDT.Distributed.Services;
using Ama.Enterprise.Samples.Migration.Models;

/// <summary>
/// Implementation of <see cref="IFeatureFlagV2CrdtManager"/> for managing V2 feature flag rules.
/// </summary>
public sealed class FeatureFlagV2CrdtManager : IFeatureFlagV2CrdtManager
{
    private readonly ICrdtDocumentOrchestrator orchestrator;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureFlagV2CrdtManager"/> class.
    /// </summary>
    /// <param name="orchestrator">The CRDT document orchestrator.</param>
    public FeatureFlagV2CrdtManager(ICrdtDocumentOrchestrator orchestrator)
    {
        ArgumentNullException.ThrowIfNull(orchestrator);
        this.orchestrator = orchestrator;
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyDictionary<string, FeatureFlagRuleV2>> GetRulesAsync(CancellationToken cancellationToken = default)
    {
        var docId = Ama.Enterprise.FeatureFlags.Constants.GlobalDocumentId;
        if (!await orchestrator.DocumentExistsAsync<FeatureFlagStateV2>(docId, cancellationToken).ConfigureAwait(false))
        {
            return new ReadOnlyDictionary<string, FeatureFlagRuleV2>(new Dictionary<string, FeatureFlagRuleV2>(StringComparer.Ordinal));
        }

        var crdtDoc = await orchestrator.GetDocumentStateAsync<FeatureFlagStateV2>(docId, cancellationToken).ConfigureAwait(false);
        if (crdtDoc.Data?.Rules is not null)
        {
            return new ReadOnlyDictionary<string, FeatureFlagRuleV2>(crdtDoc.Data.Rules);
        }

        return new ReadOnlyDictionary<string, FeatureFlagRuleV2>(new Dictionary<string, FeatureFlagRuleV2>(StringComparer.Ordinal));
    }

    /// <inheritdoc />
    public async Task SetRuleAsync(
        string key,
        int rolloutPercentage,
        string? owner = null,
        IntentDeliveryMode deliveryMode = IntentDeliveryMode.Acknowledged,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var clampedRollout = Math.Clamp(rolloutPercentage, 0, 100);
        var state = clampedRollout switch
        {
            0 => "Inactive",
            100 => "Active",
            _ => "Canary"
        };

        var docId = Ama.Enterprise.FeatureFlags.Constants.GlobalDocumentId;
        if (!await orchestrator.DocumentExistsAsync<FeatureFlagStateV2>(docId, cancellationToken).ConfigureAwait(false))
        {
            await orchestrator.CreateDocumentAsync(docId, Constants.FeatureFlagV2DocumentType, deliveryMode, cancellationToken).ConfigureAwait(false);
        }

        var rule = new FeatureFlagRuleV2
        {
            Key = key,
            State = state,
            RolloutPercentage = clampedRollout,
            Owner = string.IsNullOrWhiteSpace(owner) ? "v2-console" : owner,
            SchemaVersion = 2,
            UpdatedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        var intent = new MapSetIntent(key, rule);
        await orchestrator.ApplyIntentAsync<FeatureFlagStateV2>(
            docId,
            intent,
            nameof(FeatureFlagStateV2.Rules),
            deliveryMode,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemoveRuleAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var docId = Ama.Enterprise.FeatureFlags.Constants.GlobalDocumentId;
        var intent = new MapRemoveIntent(key);
        await orchestrator.ApplyIntentAsync<FeatureFlagStateV2>(
            docId,
            intent,
            nameof(FeatureFlagStateV2.Rules),
            IntentDeliveryMode.Acknowledged,
            cancellationToken).ConfigureAwait(false);
    }
}
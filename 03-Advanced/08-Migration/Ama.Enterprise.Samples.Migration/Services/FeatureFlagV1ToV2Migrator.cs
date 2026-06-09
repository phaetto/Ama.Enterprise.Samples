namespace Ama.Enterprise.Samples.Migration.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using Ama.CRDT.Models;
using Ama.CRDT.Models.Intents;
using Ama.Enterprise.CRDT.Distributed.Models;
using Ama.Enterprise.CRDT.Distributed.Services;
using Ama.Enterprise.FeatureFlags.Models;
using Ama.Enterprise.Samples.Migration.Models;

/// <summary>
/// Live CRDT document migrator that translates Legacy V1 <see cref="FeatureFlagState"/> documents
/// into breaking Modern V2 <see cref="FeatureFlagStateV2"/> documents.
/// </summary>
public sealed class FeatureFlagV1ToV2Migrator : ICrdtDocumentMigrator<FeatureFlagState, FeatureFlagStateV2>
{
    /// <inheritdoc />
    public async Task MigrateDocumentAsync(
        string targetDocumentId,
        CrdtDocument<FeatureFlagState> sourceDocument,
        CrdtDocument<FeatureFlagState>? previousSourceDocument,
        CrdtDocument<FeatureFlagStateV2> targetDocument,
        ICrdtDocumentOrchestrator targetOrchestrator,
        IntentDeliveryMode deliveryMode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDocumentId);
        ArgumentNullException.ThrowIfNull(targetOrchestrator);

        if (sourceDocument.Data?.Flags is null)
        {
            return;
        }

        var existingRules = targetDocument.Data?.Rules;
        var previousFlags = previousSourceDocument?.Data?.Flags;

        // 1. Upsert added or modified flags from V1 (Flags) -> V2 (Rules)
        foreach (var kvp in sourceDocument.Data.Flags)
        {
            var v1Flag = kvp.Value;
            var owner = v1Flag.Ownership?.Owner
                        ?? v1Flag.Audit?.LastModifiedBy
                        ?? "migrated-from-v1";

            var updatedAtMs = v1Flag.Audit is not null && v1Flag.Audit.UpdatedAt != default
                ? v1Flag.Audit.UpdatedAt.ToUnixTimeMilliseconds()
                : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var expectedV2 = new FeatureFlagRuleV2
            {
                Key = v1Flag.Name,
                State = v1Flag.IsEnabled ? "Active" : "Inactive",
                RolloutPercentage = v1Flag.IsEnabled ? 100 : 0,
                Owner = owner,
                SchemaVersion = 2,
                UpdatedAtUnixMs = updatedAtMs
            };

            if (existingRules is not null &&
                existingRules.TryGetValue(kvp.Key, out var currentV2) &&
                currentV2.Equals(expectedV2))
            {
                continue;
            }

            await targetOrchestrator.ApplyIntentAsync<FeatureFlagStateV2>(
                targetDocumentId,
                new MapSetIntent(kvp.Key, expectedV2),
                nameof(FeatureFlagStateV2.Rules),
                deliveryMode,
                cancellationToken).ConfigureAwait(false);
        }

        // 2. Three-way merge removal: only delete from V2 if the key previously existed in V1
        // and was deleted from V1, preserving rules created directly in V2.
        if (previousFlags is not null && existingRules is not null)
        {
            foreach (var previousKey in previousFlags.Keys)
            {
                if (!sourceDocument.Data.Flags.ContainsKey(previousKey) &&
                    existingRules.ContainsKey(previousKey))
                {
                    await targetOrchestrator.ApplyIntentAsync<FeatureFlagStateV2>(
                        targetDocumentId,
                        new MapRemoveIntent(previousKey),
                        nameof(FeatureFlagStateV2.Rules),
                        deliveryMode,
                        cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
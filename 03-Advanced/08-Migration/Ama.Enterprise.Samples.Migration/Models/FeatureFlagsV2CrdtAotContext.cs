namespace Ama.Enterprise.Samples.Migration.Models;

using System.Collections.Generic;
using Ama.CRDT.Attributes;
using Ama.CRDT.Models;
using Ama.CRDT.Models.Aot;

/// <summary>
/// AOT reflection context for the V2 Feature Flags CRDT models.
/// </summary>
[CrdtAotType(typeof(FeatureFlagRuleV2))]
[CrdtAotType(typeof(FeatureFlagStateV2))]
[CrdtAotType(typeof(IDictionary<string, FeatureFlagRuleV2>))]
[CrdtAotType(typeof(Dictionary<string, FeatureFlagRuleV2>))]
[CrdtAotType(typeof(IDictionary<Guid, CausalTimestamp>))]
public sealed partial class FeatureFlagsV2CrdtAotContext : CrdtAotContext
{
}
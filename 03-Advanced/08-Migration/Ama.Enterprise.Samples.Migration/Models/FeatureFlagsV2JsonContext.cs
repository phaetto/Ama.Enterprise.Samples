namespace Ama.Enterprise.Samples.Migration.Models;

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Ama.CRDT.Models;

/// <summary>
/// AOT-friendly JSON context for V2 Feature Flags serialization.
/// </summary>
[JsonSerializable(typeof(FeatureFlagRuleV2))]
[JsonSerializable(typeof(FeatureFlagStateV2))]
[JsonSerializable(typeof(CrdtDocument<FeatureFlagStateV2>))]
[JsonSerializable(typeof(IDictionary<string, FeatureFlagRuleV2>))]
[JsonSerializable(typeof(Dictionary<string, FeatureFlagRuleV2>))]
public sealed partial class FeatureFlagsV2JsonContext : JsonSerializerContext
{
}
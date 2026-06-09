namespace Ama.Enterprise.Samples.Sharding.Models;

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Ama.CRDT.Models;

/// <summary>
/// Native AOT <see cref="JsonSerializerContext"/> for serializing sharded tenant feature flag states.
/// </summary>
[JsonSerializable(typeof(TenantFeatureFlagState))]
[JsonSerializable(typeof(CrdtDocument<TenantFeatureFlagState>))]
[JsonSerializable(typeof(IDictionary<string, bool>))]
[JsonSerializable(typeof(Dictionary<string, bool>))]
public sealed partial class ShardingJsonContext : JsonSerializerContext
{
}
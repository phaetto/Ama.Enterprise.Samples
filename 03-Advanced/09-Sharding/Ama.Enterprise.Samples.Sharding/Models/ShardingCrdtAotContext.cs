namespace Ama.Enterprise.Samples.Sharding.Models;

using System.Collections.Generic;
using Ama.CRDT.Attributes;
using Ama.CRDT.Models.Aot;

/// <summary>
/// Native AOT CRDT reflection context for the Horizontal Sharding showcase models.
/// </summary>
[CrdtAotType(typeof(TenantFeatureFlagState))]
[CrdtAotType(typeof(IDictionary<string, bool>))]
[CrdtAotType(typeof(Dictionary<string, bool>))]
public sealed partial class ShardingCrdtAotContext : CrdtAotContext
{
}
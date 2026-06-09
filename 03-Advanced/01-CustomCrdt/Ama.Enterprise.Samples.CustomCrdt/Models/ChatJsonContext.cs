namespace Ama.Enterprise.Samples.CreateCrdt.Models;

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Ama.CRDT.Models;
using Ama.Enterprise.Samples.CustomCrdt.Models;

/// <summary>
/// Source-generated System.Text.Json context for AOT-compatible serialization of the Chat CRDT models.
/// </summary>
[JsonSerializable(typeof(ChatChannelState))]
[JsonSerializable(typeof(ChatMessageState))]
[JsonSerializable(typeof(ChatItemDefinition))]
[JsonSerializable(typeof(IList<ChatItemDefinition>))]
[JsonSerializable(typeof(List<ChatItemDefinition>))]
[JsonSerializable(typeof(IDictionary<string, ChatMessageState>))]
[JsonSerializable(typeof(Dictionary<string, ChatMessageState>))]
[JsonSerializable(typeof(ISet<string>))]
[JsonSerializable(typeof(HashSet<string>))]
[JsonSerializable(typeof(CrdtDocument<ChatChannelState>))]
internal sealed partial class ChatJsonContext : JsonSerializerContext
{
}
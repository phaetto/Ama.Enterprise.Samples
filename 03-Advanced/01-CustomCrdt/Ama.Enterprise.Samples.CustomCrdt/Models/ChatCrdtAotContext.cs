namespace Ama.Enterprise.Samples.CreateCrdt.Models;

using System.Collections.Generic;
using Ama.CRDT.Attributes;
using Ama.CRDT.Models.Aot;
using Ama.Enterprise.Samples.CustomCrdt.Models;

/// <summary>
/// Source-generated AOT reflection context for the custom Chat CRDT models.
/// </summary>
[CrdtAotType(typeof(ChatChannelState))]
[CrdtAotType(typeof(ChatMessageState))]
[CrdtAotType(typeof(ChatItemDefinition))]
[CrdtAotType(typeof(IList<ChatItemDefinition>))]
[CrdtAotType(typeof(List<ChatItemDefinition>))]
[CrdtAotType(typeof(IDictionary<string, ChatMessageState>))]
[CrdtAotType(typeof(Dictionary<string, ChatMessageState>))]
[CrdtAotType(typeof(ISet<string>))]
[CrdtAotType(typeof(HashSet<string>))]
internal sealed partial class ChatCrdtAotContext : CrdtAotContext
{
}
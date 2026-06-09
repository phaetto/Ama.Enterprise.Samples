namespace Ama.Enterprise.Samples.CustomCrdt.Extensions;

using System;
using Ama.CRDT.Extensions;
using Ama.Enterprise.CRDT.Distributed.Extensions;
using Ama.Enterprise.CRDT.Distributed.Services.Observers;
using Ama.Enterprise.Samples.CreateCrdt.Models;
using Ama.Enterprise.Samples.CustomCrdt;
using Ama.Enterprise.Samples.CustomCrdt.Models;
using Ama.Enterprise.Samples.CustomCrdt.Services;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency injection extensions for registering the custom Chat CRDT document, document ID provider, priority queue comparer, AOT contexts, and manager.
/// </summary>
public static class ChatServiceCollectionExtensions
{
    /// <summary>
    /// Registers the custom Chat CRDT models, <see cref="ChatDocumentIdProvider"/>, element comparer, JSON/AOT serialization contexts, state observer, and <see cref="IChatCrdtManager"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddChatMessengerCrdt(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddCrdt()
            .AddCrdtJsonTypeInfoResolver(ChatJsonContext.Default)
            .AddCrdtAotContext<ChatCrdtAotContext>()
            .AddCrdtDocumentIdProvider<ChatDocumentIdProvider>()
            .AddCrdtComparer<ChatItemElementComparer>()
            .AddCrdtSerializableType<ChatItemDefinition>(Constants.ChatItemDefinitionDiscriminator)
            .AddCrdtSerializableType<ChatMessageState>(Constants.ChatMessageStateDiscriminator)
            .AddCrdtSystemTextJson();

        services.AddDistributedDocumentType<ChatChannelState>(Constants.ChatChannelDocumentType);
        services.AddDistributedCrdtService<IChatCrdtManager, ChatCrdtManager>();
        services.AddSingleton<IDocumentStateObserver<ChatChannelState>, ConsoleChatChannelStateObserver>();

        return services;
    }
}
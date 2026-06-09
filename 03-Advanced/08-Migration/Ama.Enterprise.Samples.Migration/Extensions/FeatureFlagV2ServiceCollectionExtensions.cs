namespace Ama.Enterprise.Samples.Migration.Extensions;

using System;
using Ama.CRDT.Extensions;
using Ama.Enterprise.CRDT.Distributed.Extensions;
using Ama.Enterprise.CRDT.Distributed.Services.Observers;
using Ama.Enterprise.Samples.Migration.Models;
using Ama.Enterprise.Samples.Migration.Services;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency injection extensions for registering the breaking V2 Feature Flags schema and services.
/// </summary>
public static class FeatureFlagV2ServiceCollectionExtensions
{
    /// <summary>
    /// Registers V2 Feature Flag CRDT models, AOT contexts, document types, and scoped managers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFeatureFlagsV2(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddCrdt()
            .AddCrdtAotContext<FeatureFlagsV2CrdtAotContext>()
            .AddCrdtSerializableType<FeatureFlagRuleV2>(Constants.FeatureFlagV2PayloadType)
            .AddCrdtJsonTypeInfoResolver(FeatureFlagsV2JsonContext.Default);

        services.AddDistributedDocumentType<FeatureFlagStateV2>(Constants.FeatureFlagV2DocumentType);
        services.AddDistributedCrdtService<IFeatureFlagV2CrdtManager, FeatureFlagV2CrdtManager>();
        services.AddSingleton<IDocumentStateObserver<FeatureFlagStateV2>, ConsoleFeatureFlagV2Observer>();

        return services;
    }
}
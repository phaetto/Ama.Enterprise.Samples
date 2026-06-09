namespace Ama.Enterprise.Samples.Sharding.Extensions;

using System;
using Ama.CRDT.Extensions;
using Ama.Enterprise.CRDT.Distributed.Extensions;
using Ama.Enterprise.CRDT.Distributed.Services.Observers;
using Ama.Enterprise.Samples.Sharding.Models;
using Ama.Enterprise.Samples.Sharding.Services;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency injection extension methods for registering the sharded tenant feature flags domain services.
/// </summary>
public static class ShardingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the sharded <see cref="TenantFeatureFlagState"/> CRDT document type, Native AOT contexts,
    /// <see cref="TenantShardIdProvider"/>, and <see cref="ITenantFlagManager"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddShardedTenantFeatureFlags(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ConsoleShardingObserver>();
        services.AddSingleton<IDocumentStateObserver<TenantFeatureFlagState>>(sp => sp.GetRequiredService<ConsoleShardingObserver>());
        services.AddSingleton<ICrdtOrchestratorObserver>(sp => sp.GetRequiredService<ConsoleShardingObserver>());

        services.AddCrdt()
                .AddCrdtAotContext(new ShardingCrdtAotContext())
                .AddCrdtJsonTypeInfoResolver(ShardingJsonContext.Default)
                .AddCrdtSystemTextJson()
                .AddCrdtShardIdProvider<TenantShardIdProvider>();

        services.AddDistributedDocumentType<TenantFeatureFlagState>(Constants.TenantFlagsDocType);
        services.AddDistributedCrdtService<ITenantFlagManager, TenantFlagManager>();

        return services;
    }
}
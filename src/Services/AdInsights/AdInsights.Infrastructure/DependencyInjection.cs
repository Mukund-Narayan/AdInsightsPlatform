using AdInsights.Application.Common.Behaviors;
using AdInsights.Application.Common.Interfaces;
using AdInsights.Domain.Repositories;
using AdInsights.Infrastructure.Caching;
using AdInsights.Infrastructure.Context;
using AdInsights.Infrastructure.Persistence.Cassandra;
using AdInsights.Infrastructure.Persistence.ClickHouse;
using AdInsights.Infrastructure.Persistence.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace AdInsights.Infrastructure;

/// <summary>
/// Infrastructure layer DI registration — the single entry point for wiring
/// all concrete implementations to their interfaces.
///
/// Design: All external dependencies (Cassandra, Redis, ClickHouse) are registered
/// here, keeping the API startup lean and the DI graph explicit.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all infrastructure services: repositories, caching, tenant context.
    /// Call from <c>Program.cs</c>: <c>builder.Services.AddInfrastructure(configuration)</c>
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCassandra(configuration);
        services.AddRedisCache(configuration);
        services.AddRepositories();
        services.AddTenantContext();

        return services;
    }

    private static void AddCassandra(this IServiceCollection services, IConfiguration configuration)
    {
        // Singleton: ISession is thread-safe and connection-pooled by the driver
        services.AddSingleton<CassandraConnectionFactory>();
        services.AddSingleton<CassandraAdMetricsRepository>();
    }

    private static void AddRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? "localhost:6379";

        // IConnectionMultiplexer is thread-safe and designed as a singleton
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddSingleton<RedisCacheRepository>();
        services.AddSingleton<ICacheRepository>(sp => sp.GetRequiredService<RedisCacheRepository>());
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddSingleton<ClickHouseAdMetricsRepository>();

        // HybridAdMetricsRepository is the only concrete class bound to IAdMetricsRepository.
        // It transparently routes to Cassandra or ClickHouse based on the time period.
        services.AddSingleton<IAdMetricsRepository, HybridAdMetricsRepository>();
    }

    private static void AddTenantContext(this IServiceCollection services)
    {
        // Scoped: one TenantContext per HTTP request, populated by TenantResolutionMiddleware
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
    }
}

using AdInsights.Domain.Repositories;
using AdInsights.Domain.ValueObjects;
using AdInsights.Infrastructure.Persistence.Cassandra;
using AdInsights.Infrastructure.Persistence.ClickHouse;
using Microsoft.Extensions.Logging;

namespace AdInsights.Infrastructure.Persistence.Routing;

/// <summary>
/// Decorator/Router implementation of <see cref="IAdMetricsRepository"/> that transparently
/// routes queries to the appropriate storage backend based on the requested time period:
///
///   - Period ≤ 30 days  → Cassandra  (hot path: sub-ms, counter tables)
///   - Period > 30 days  → ClickHouse (cold path: columnar analytics)
///   - Spans both        → Both queried concurrently, results merged (summed)
///
/// This is the only class registered against IAdMetricsRepository in the DI container.
/// Query handlers are completely unaware of which backend serves their request (DIP, OCP).
/// </summary>
public sealed class HybridAdMetricsRepository : IAdMetricsRepository
{
    private readonly CassandraAdMetricsRepository _hotRepository;
    private readonly ClickHouseAdMetricsRepository _coldRepository;
    private readonly ILogger<HybridAdMetricsRepository> _logger;

    public HybridAdMetricsRepository(
        CassandraAdMetricsRepository hotRepository,
        ClickHouseAdMetricsRepository coldRepository,
        ILogger<HybridAdMetricsRepository> logger)
    {
        _hotRepository = hotRepository;
        _coldRepository = coldRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<long> GetClicksAsync(
        string campaignId,
        string tenantId,
        TimePeriod period,
        CancellationToken cancellationToken)
        => RouteQueryAsync(
            campaignId, tenantId, period, cancellationToken,
            (repo, id, tid, p, ct) => repo.GetClicksAsync(id, tid, p, ct),
            "clicks");

    /// <inheritdoc />
    public Task<long> GetImpressionsAsync(
        string campaignId,
        string tenantId,
        TimePeriod period,
        CancellationToken cancellationToken)
        => RouteQueryAsync(
            campaignId, tenantId, period, cancellationToken,
            (repo, id, tid, p, ct) => repo.GetImpressionsAsync(id, tid, p, ct),
            "impressions");

    /// <inheritdoc />
    public Task<long> GetClickToBasketAsync(
        string campaignId,
        string tenantId,
        TimePeriod period,
        CancellationToken cancellationToken)
        => RouteQueryAsync(
            campaignId, tenantId, period, cancellationToken,
            (repo, id, tid, p, ct) => repo.GetClickToBasketAsync(id, tid, p, ct),
            "ctb");

    private async Task<long> RouteQueryAsync(
        string campaignId,
        string tenantId,
        TimePeriod period,
        CancellationToken cancellationToken,
        Func<IAdMetricsRepository, string, string, TimePeriod, CancellationToken, Task<long>> queryFunc,
        string metricLabel)
    {
        if (period.IsRealTime())
        {
            _logger.LogDebug("Routing {Metric} to Cassandra (hot path)", metricLabel);
            return await queryFunc(_hotRepository, campaignId, tenantId, period, cancellationToken);
        }

        if (period.IsHistorical())
        {
            _logger.LogDebug("Routing {Metric} to ClickHouse (cold path)", metricLabel);
            return await queryFunc(_coldRepository, campaignId, tenantId, period, cancellationToken);
        }

        // Period spans both hot and cold — query both concurrently and sum results
        _logger.LogDebug("Routing {Metric} to both Cassandra + ClickHouse (spanning both paths)", metricLabel);

        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);
        var hotPeriod = new TimePeriod(cutoff, period.To);
        var coldPeriod = new TimePeriod(period.From, cutoff.AddSeconds(-1));

        var hotTask = queryFunc(_hotRepository, campaignId, tenantId, hotPeriod, cancellationToken);
        var coldTask = queryFunc(_coldRepository, campaignId, tenantId, coldPeriod, cancellationToken);

        var results = await Task.WhenAll(hotTask, coldTask);
        return results[0] + results[1];
    }
}

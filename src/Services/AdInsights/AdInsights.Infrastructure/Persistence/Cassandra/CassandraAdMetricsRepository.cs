using AdInsights.Domain.Repositories;
using AdInsights.Domain.ValueObjects;
using Cassandra;
using Microsoft.Extensions.Logging;

namespace AdInsights.Infrastructure.Persistence.Cassandra;

/// <summary>
/// Apache Cassandra implementation of <see cref="IAdMetricsRepository"/> for the hot path (≤30 days).
///
/// Schema: ad_insights.campaign_metrics
///   PRIMARY KEY ((tenant_id, campaign_id, metric_type), bucket_start)
///   default_time_to_live = 2592000  -- 30 days
///
/// Uses prepared statements to avoid re-parsing CQL on every query (CQ-046).
/// All queries are scoped to (tenant_id, campaign_id) to enforce data isolation.
/// </summary>
public sealed class CassandraAdMetricsRepository : IAdMetricsRepository
{
    private const string ClicksMetricType = "CLICKS";
    private const string ImpressionsMetricType = "IMPRESSIONS";
    private const string ClickToBasketMetricType = "CLICK_TO_BASKET";

    private readonly ISession _session;
    private readonly ILogger<CassandraAdMetricsRepository> _logger;
    private readonly PreparedStatement _getMetricStatement;

    public CassandraAdMetricsRepository(
        CassandraConnectionFactory connectionFactory,
        ILogger<CassandraAdMetricsRepository> logger)
    {
        _session = connectionFactory.CreateSession();
        _logger = logger;

        // Prepared statement avoids CQL re-parsing (performance + injection prevention)
        _getMetricStatement = _session.Prepare(@"
            SELECT SUM(count) AS total
            FROM campaign_metrics
            WHERE tenant_id = ?
              AND campaign_id = ?
              AND metric_type = ?
              AND bucket_start >= ?
              AND bucket_start <= ?");
    }

    /// <inheritdoc />
    public Task<long> GetClicksAsync(
        string campaignId,
        string tenantId,
        TimePeriod period,
        CancellationToken cancellationToken)
        => ExecuteMetricQueryAsync(campaignId, tenantId, ClicksMetricType, period, cancellationToken);

    /// <inheritdoc />
    public Task<long> GetImpressionsAsync(
        string campaignId,
        string tenantId,
        TimePeriod period,
        CancellationToken cancellationToken)
        => ExecuteMetricQueryAsync(campaignId, tenantId, ImpressionsMetricType, period, cancellationToken);

    /// <inheritdoc />
    public Task<long> GetClickToBasketAsync(
        string campaignId,
        string tenantId,
        TimePeriod period,
        CancellationToken cancellationToken)
        => ExecuteMetricQueryAsync(campaignId, tenantId, ClickToBasketMetricType, period, cancellationToken);

    private async Task<long> ExecuteMetricQueryAsync(
        string campaignId,
        string tenantId,
        string metricType,
        TimePeriod period,
        CancellationToken cancellationToken)
    {
        var bound = _getMetricStatement.Bind(
            tenantId,
            campaignId,
            metricType,
            period.From.ToUnixTimeMilliseconds(),
            period.To.ToUnixTimeMilliseconds());

        _logger.LogDebug(
            "Cassandra query: tenant={TenantId} campaign={CampaignId} metric={MetricType}",
            tenantId, campaignId, metricType);

        var rowSet = await _session.ExecuteAsync(bound);
        var row = rowSet.FirstOrDefault();

        return row?.GetValue<long?>("total") ?? 0L;
    }
}

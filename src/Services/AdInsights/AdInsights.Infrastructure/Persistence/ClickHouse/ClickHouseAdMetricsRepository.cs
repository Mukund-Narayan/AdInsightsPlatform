using AdInsights.Domain.Repositories;
using AdInsights.Domain.ValueObjects;
using ClickHouse.Client.ADO;
using ClickHouse.Client.Utility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AdInsights.Infrastructure.Persistence.ClickHouse;

/// <summary>
/// ClickHouse implementation of <see cref="IAdMetricsRepository"/> for the cold/historical path (>30 days).
///
/// ClickHouse's columnar MergeTree engine provides extremely fast aggregations over large time ranges —
/// orders of magnitude faster than row-oriented databases for SUM/COUNT queries.
///
/// Schema: ad_insights.campaign_events
///   ENGINE = MergeTree()
///   PARTITION BY toYYYYMM(event_time)
///   ORDER BY (tenant_id, campaign_id, metric_type, event_time)
///
/// All queries include tenant_id in the WHERE clause to enforce data isolation (CQ-050).
/// </summary>
public sealed class ClickHouseAdMetricsRepository : IAdMetricsRepository
{
    private const string ClicksMetricType = "CLICKS";
    private const string ImpressionsMetricType = "IMPRESSIONS";
    private const string ClickToBasketMetricType = "CLICK_TO_BASKET";

    private readonly string _connectionString;
    private readonly ILogger<ClickHouseAdMetricsRepository> _logger;

    public ClickHouseAdMetricsRepository(
        IConfiguration configuration,
        ILogger<ClickHouseAdMetricsRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("ClickHouse")
            ?? "Host=localhost;Port=8123;Database=ad_insights;Username=default;Password=";
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<long> GetClicksAsync(
        string campaignId,
        string tenantId,
        TimePeriod period,
        CancellationToken cancellationToken)
        => ExecuteAggregationAsync(campaignId, tenantId, ClicksMetricType, period, cancellationToken);

    /// <inheritdoc />
    public Task<long> GetImpressionsAsync(
        string campaignId,
        string tenantId,
        TimePeriod period,
        CancellationToken cancellationToken)
        => ExecuteAggregationAsync(campaignId, tenantId, ImpressionsMetricType, period, cancellationToken);

    /// <inheritdoc />
    public Task<long> GetClickToBasketAsync(
        string campaignId,
        string tenantId,
        TimePeriod period,
        CancellationToken cancellationToken)
        => ExecuteAggregationAsync(campaignId, tenantId, ClickToBasketMetricType, period, cancellationToken);

    private async Task<long> ExecuteAggregationAsync(
        string campaignId,
        string tenantId,
        string metricType,
        TimePeriod period,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "ClickHouse query: tenant={TenantId} campaign={CampaignId} metric={MetricType} from={From} to={To}",
            tenantId, campaignId, metricType, period.From, period.To);

        await using var connection = new ClickHouseConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Parameterised query prevents SQL injection
        var sql = @"
            SELECT SUM(event_count) AS total
            FROM campaign_metrics_hourly
            WHERE tenant_id = {tenantId:String}
              AND campaign_id = {campaignId:String}
              AND metric_type = {metricType:String}
              AND bucket_hour >= {fromTime:DateTime}
              AND bucket_hour <= {toTime:DateTime}";

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.AddParameter("tenantId", tenantId);
        command.AddParameter("campaignId", campaignId);
        command.AddParameter("metricType", metricType);
        command.AddParameter("fromTime", period.From.UtcDateTime);
        command.AddParameter("toTime", period.To.UtcDateTime);

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is null or DBNull ? 0L : Convert.ToInt64(result);
    }
}

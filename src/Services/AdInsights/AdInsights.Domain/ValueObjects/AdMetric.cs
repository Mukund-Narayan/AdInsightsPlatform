using AdInsights.Domain.Enums;

namespace AdInsights.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing a single aggregated advertising metric.
/// Returned from the repository layer and transformed into API responses.
/// </summary>
public sealed record AdMetric
{
    public string CampaignId { get; }

    public string TenantId { get; }

    public MetricType MetricType { get; }

    public long Count { get; }

    public TimePeriod Period { get; }

    public AdMetric(
        string campaignId,
        string tenantId,
        MetricType metricType,
        long count,
        TimePeriod period)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(campaignId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        CampaignId = campaignId;
        TenantId = tenantId;
        MetricType = metricType;
        Count = count;
        Period = period;
    }
}

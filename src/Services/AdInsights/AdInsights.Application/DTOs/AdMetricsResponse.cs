namespace AdInsights.Application.DTOs;

/// <summary>
/// Standardised API response for all ad metric queries (clicks, impressions, click-to-basket).
/// Immutable record ensures thread-safety and consistent serialisation.
/// </summary>
public sealed record AdMetricsResponse
{
    public required string CampaignId { get; init; }

    public required string MetricType { get; init; }

    public required long Count { get; init; }

    public required DateTimeOffset From { get; init; }

    public required DateTimeOffset To { get; init; }

    /// <summary>
    /// True when data was served from the real-time path (Cassandra ≤30 days).
    /// False when served from historical path (ClickHouse >30 days).
    /// </summary>
    public required bool IsRealTime { get; init; }

    /// <summary>UTC timestamp when this metric was computed / fetched.</summary>
    public DateTimeOffset ComputedAt { get; init; } = DateTimeOffset.UtcNow;
}

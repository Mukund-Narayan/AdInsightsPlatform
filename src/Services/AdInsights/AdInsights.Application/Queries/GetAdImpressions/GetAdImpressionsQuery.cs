using AdInsights.Application.Common.Interfaces;
using AdInsights.Application.DTOs;
using MediatR;

namespace AdInsights.Application.Queries.GetAdImpressions;

/// <summary>
/// Query to retrieve the total number of ad impressions for a campaign within a date range.
/// Implements <see cref="ICacheableQuery"/> for automatic Redis caching via the pipeline behavior.
/// </summary>
public sealed record GetAdImpressionsQuery : IRequest<AdMetricsResponse>, ICacheableQuery
{
    public required string CampaignId { get; init; }
    public required string TenantId { get; init; }
    public required DateTimeOffset From { get; init; }
    public required DateTimeOffset To { get; init; }

    /// <inheritdoc />
    public string CacheKey =>
        $"metrics:impressions:{TenantId}:{CampaignId}:{From:yyyyMMddHH}:{To:yyyyMMddHH}";

    /// <inheritdoc />
    public TimeSpan CacheDuration =>
        (DateTimeOffset.UtcNow - From).TotalDays <= 30
            ? TimeSpan.FromSeconds(30)
            : TimeSpan.FromMinutes(5);
}

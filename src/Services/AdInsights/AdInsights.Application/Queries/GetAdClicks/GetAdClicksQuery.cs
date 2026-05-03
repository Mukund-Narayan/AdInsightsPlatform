using AdInsights.Application.Common.Interfaces;
using AdInsights.Application.DTOs;
using MediatR;

namespace AdInsights.Application.Queries.GetAdClicks;

/// <summary>
/// Query to retrieve the total number of ad clicks for a campaign within a date range.
/// Implements <see cref="ICacheableQuery"/> so the <c>CachingBehavior</c> automatically
/// caches the result in Redis with the configured TTL.
/// </summary>
public sealed record GetAdClicksQuery : IRequest<AdMetricsResponse>, ICacheableQuery
{
    public required string CampaignId { get; init; }

    public required string TenantId { get; init; }
    
    public required DateTimeOffset From { get; init; }
    
    public required DateTimeOffset To { get; init; }
    
    public string CacheKey => $"metrics:clicks:{TenantId}:{CampaignId}:{From:yyyyMMddHH}:{To:yyyyMMddHH}";

    /// <inheritdoc />
    public TimeSpan CacheDuration =>
        (DateTimeOffset.UtcNow - From).TotalDays <= 30
            ? TimeSpan.FromSeconds(30)   // Real-time: short TTL keeps data fresh
            : TimeSpan.FromMinutes(5);   // Historical: longer TTL, data doesn't change
}

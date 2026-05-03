using AdInsights.Application.Common.Interfaces;
using AdInsights.Application.DTOs;
using MediatR;

namespace AdInsights.Application.Queries.GetClickToBasket;

/// <summary>
/// Query to retrieve click-to-basket conversion counts for a campaign.
/// A conversion is recorded when a user adds a product to cart within 30 minutes of clicking an ad,
/// correlated by Flink CEP using the userId + campaignId session key.
/// </summary>
public sealed record GetClickToBasketQuery : IRequest<AdMetricsResponse>, ICacheableQuery
{
    public required string CampaignId { get; init; }
    public required string TenantId { get; init; }
    public required DateTimeOffset From { get; init; }
    public required DateTimeOffset To { get; init; }

    /// <inheritdoc />
    public string CacheKey =>
        $"metrics:ctb:{TenantId}:{CampaignId}:{From:yyyyMMddHH}:{To:yyyyMMddHH}";

    /// <inheritdoc />
    public TimeSpan CacheDuration =>
        (DateTimeOffset.UtcNow - From).TotalDays <= 30
            ? TimeSpan.FromSeconds(30)
            : TimeSpan.FromMinutes(5);
}

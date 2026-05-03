using AdInsights.Application.DTOs;
using AdInsights.Domain.Enums;
using AdInsights.Domain.Exceptions;
using AdInsights.Domain.Repositories;
using AdInsights.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AdInsights.Application.Queries.GetAdClicks;

/// <summary>
/// Handles the <see cref="GetAdClicksQuery"/> by querying the ad metrics repository.
/// The <c>CachingBehavior</c> pipeline wraps this handler, so it only executes on cache misses.
///
/// SOLID compliance:
///   - SRP: This handler has exactly one responsibility — resolve click counts.
///   - OCP: New storage strategies (e.g. adding a new data source) extend HybridRepository,
///           not this handler.
///   - DIP: Depends on IAdMetricsRepository interface, not a concrete class.
/// </summary>
public sealed class GetAdClicksQueryHandler : IRequestHandler<GetAdClicksQuery, AdMetricsResponse>
{
    private readonly IAdMetricsRepository _metricsRepository;
    private readonly ILogger<GetAdClicksQueryHandler> _logger;

    public GetAdClicksQueryHandler(
        IAdMetricsRepository metricsRepository,
        ILogger<GetAdClicksQueryHandler> logger)
    {
        _metricsRepository = metricsRepository;
        _logger = logger;
    }

    /// <summary>
    /// Executes the click count query against the appropriate storage backend
    /// (routed transparently by HybridAdMetricsRepository).
    /// </summary>
    public async Task<AdMetricsResponse> Handle(
        GetAdClicksQuery query,
        CancellationToken cancellationToken)
    {
        var period = new TimePeriod(query.From, query.To);

        _logger.LogInformation(
            "Fetching click count for Campaign={CampaignId} Tenant={TenantId} Period={From}-{To}",
            query.CampaignId, query.TenantId, query.From, query.To);

        var clicks = await _metricsRepository.GetClicksAsync(
            query.CampaignId,
            query.TenantId,
            period,
            cancellationToken);

        return new AdMetricsResponse
        {
            CampaignId = query.CampaignId,
            MetricType = MetricType.Clicks.ToString(),
            Count = clicks,
            From = query.From,
            To = query.To,
            IsRealTime = period.IsRealTime()
        };
    }
}

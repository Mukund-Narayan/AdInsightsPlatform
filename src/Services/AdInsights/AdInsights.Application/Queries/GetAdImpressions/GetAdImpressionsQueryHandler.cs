using AdInsights.Application.DTOs;
using AdInsights.Domain.Enums;
using AdInsights.Domain.Repositories;
using AdInsights.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AdInsights.Application.Queries.GetAdImpressions;

/// <summary>
/// Handles <see cref="GetAdImpressionsQuery"/> by fetching impression counts from the
/// appropriate storage backend via the <see cref="IAdMetricsRepository"/> abstraction.
/// </summary>
public sealed class GetAdImpressionsQueryHandler : IRequestHandler<GetAdImpressionsQuery, AdMetricsResponse>
{
    private readonly IAdMetricsRepository _metricsRepository;
    private readonly ILogger<GetAdImpressionsQueryHandler> _logger;

    public GetAdImpressionsQueryHandler(
        IAdMetricsRepository metricsRepository,
        ILogger<GetAdImpressionsQueryHandler> logger)
    {
        _metricsRepository = metricsRepository;
        _logger = logger;
    }

    public async Task<AdMetricsResponse> Handle(
        GetAdImpressionsQuery query,
        CancellationToken cancellationToken)
    {
        var period = new TimePeriod(query.From, query.To);

        _logger.LogInformation(
            "Fetching impression count for Campaign={CampaignId} Tenant={TenantId} Period={From}-{To}",
            query.CampaignId, query.TenantId, query.From, query.To);

        var impressions = await _metricsRepository.GetImpressionsAsync(
            query.CampaignId,
            query.TenantId,
            period,
            cancellationToken);

        return new AdMetricsResponse
        {
            CampaignId = query.CampaignId,
            MetricType = MetricType.Impressions.ToString(),
            Count = impressions,
            From = query.From,
            To = query.To,
            IsRealTime = period.IsRealTime()
        };
    }
}

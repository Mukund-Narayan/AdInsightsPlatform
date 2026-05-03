using AdInsights.Application.DTOs;
using AdInsights.Domain.Enums;
using AdInsights.Domain.Repositories;
using AdInsights.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AdInsights.Application.Queries.GetClickToBasket;

/// <summary>
/// Handles <see cref="GetClickToBasketQuery"/> by fetching Click To Basket conversion counts.
/// CTB data is produced by the Flink CEP ClickToBasketCorrelator job,
/// which matches AdClick events with AddToCart events within a 30-minute session window.
/// </summary>
public sealed class GetClickToBasketQueryHandler : IRequestHandler<GetClickToBasketQuery, AdMetricsResponse>
{
    private readonly IAdMetricsRepository _metricsRepository;
    private readonly ILogger<GetClickToBasketQueryHandler> _logger;

    public GetClickToBasketQueryHandler(
        IAdMetricsRepository metricsRepository,
        ILogger<GetClickToBasketQueryHandler> logger)
    {
        _metricsRepository = metricsRepository;
        _logger = logger;
    }

    public async Task<AdMetricsResponse> Handle(
        GetClickToBasketQuery query,
        CancellationToken cancellationToken)
    {
        var period = new TimePeriod(query.From, query.To);

        _logger.LogInformation(
            "Fetching CTB count for Campaign={CampaignId} Tenant={TenantId} Period={From}-{To}",
            query.CampaignId, query.TenantId, query.From, query.To);

        var ctbCount = await _metricsRepository.GetClickToBasketAsync(
            query.CampaignId,
            query.TenantId,
            period,
            cancellationToken);

        return new AdMetricsResponse
        {
            CampaignId = query.CampaignId,
            MetricType = MetricType.ClickToBasket.ToString(),
            Count = ctbCount,
            From = query.From,
            To = query.To,
            IsRealTime = period.IsRealTime()
        };
    }
}

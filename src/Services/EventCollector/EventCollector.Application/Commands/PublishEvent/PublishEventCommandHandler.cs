using AdInsightsPlatform.Contracts.Constants;
using AdInsightsPlatform.Contracts.Events;
using EventCollector.Application.DTOs;
using EventCollector.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventCollector.Application.Commands.PublishEvent;

/// <summary>
/// Handles the <see cref="PublishEventCommand"/> by mapping the inbound request
/// to the appropriate typed event and publishing it to Kafka via <see cref="IEventPublisher"/>.
///
/// Supports extensibility: adding a new event type requires adding a new mapping case here
/// without modifying the endpoint, infrastructure, or domain layers (OCP).
/// </summary>
public sealed class PublishEventCommandHandler : IRequestHandler<PublishEventCommand, EventIngestionResponse>
{
    private readonly IEventPublisher _publisher;
    private readonly ILogger<PublishEventCommandHandler> _logger;

    public PublishEventCommandHandler(
        IEventPublisher publisher,
        ILogger<PublishEventCommandHandler> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<EventIngestionResponse> Handle(
        PublishEventCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;
        var tenantId = command.TenantId;
        var eventId = Guid.NewGuid();

        _logger.LogInformation(
            "Processing {EventType} for Tenant={TenantId} Campaign={CampaignId}",
            request.EventType, tenantId, request.CampaignId);

        await PublishTypedEventAsync(request, tenantId, eventId, cancellationToken);

        return new EventIngestionResponse
        {
            EventId = eventId,
            Accepted = true
        };
    }

    private Task PublishTypedEventAsync(
        EventIngestionRequest request,
        string tenantId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        return request.EventType switch
        {
            EventTypes.AdClick => _publisher.PublishAsync(new AdClickEvent
            {
                EventId = eventId,
                TenantId = tenantId,
                CampaignId = request.CampaignId,
                AdId = request.AdId ?? string.Empty,
                UserId = request.UserId,
                ProductId = request.ProductId,
                Timestamp = request.ClientTimestamp ?? DateTimeOffset.UtcNow
            }, cancellationToken),

            EventTypes.AdImpression => _publisher.PublishAsync(new AdImpressionEvent
            {
                EventId = eventId,
                TenantId = tenantId,
                CampaignId = request.CampaignId,
                AdId = request.AdId ?? string.Empty,
                UserId = request.UserId,
                Timestamp = request.ClientTimestamp ?? DateTimeOffset.UtcNow
            }, cancellationToken),

            EventTypes.AddToCart => _publisher.PublishAsync(new AddToCartEvent
            {
                EventId = eventId,
                TenantId = tenantId,
                CampaignId = request.CampaignId,
                ProductId = request.ProductId ?? string.Empty,
                UserId = request.UserId,
                Timestamp = request.ClientTimestamp ?? DateTimeOffset.UtcNow
            }, cancellationToken),

            _ => Task.CompletedTask  // Unknown event types are silently dropped (logged by caller)
        };
    }
}

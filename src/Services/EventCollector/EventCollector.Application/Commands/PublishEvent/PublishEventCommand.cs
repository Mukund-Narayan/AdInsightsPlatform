using EventCollector.Application.DTOs;
using MediatR;

namespace EventCollector.Application.Commands.PublishEvent;

/// <summary>
/// MediatR command that encapsulates an inbound event ingestion request.
/// Maps the raw HTTP request to a domain-level command before Kafka publishing.
/// </summary>
public sealed record PublishEventCommand : IRequest<EventIngestionResponse>
{
    public required EventIngestionRequest Request { get; init; }

    /// <summary>
    /// The tenant identifier resolved from JWT — takes precedence over the request body TenantId.
    /// </summary>
    public required string TenantId { get; init; }
}

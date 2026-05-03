namespace EventCollector.Application.DTOs;

/// <summary>
/// Response returned after successfully accepting an event for processing.
/// The event is guaranteed to be durably stored in Kafka after this response.
/// </summary>
public sealed record EventIngestionResponse
{
    /// <summary>Unique identifier assigned to this event.</summary>
    public required Guid EventId { get; init; }

    /// <summary>Whether the event was accepted and published to Kafka.</summary>
    public required bool Accepted { get; init; }

    /// <summary>Server-side timestamp when the event was received.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

namespace AdInsightsPlatform.Contracts.Events;

/// <summary>
/// Base contract for all domain events published to Kafka.
/// </summary>
public abstract record BaseEvent
{
    /// <summary>Unique identifier for this event instance.</summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    public abstract string EventType { get; }

    public required string TenantId { get; init; }

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public string? CorrelationId { get; init; }
}

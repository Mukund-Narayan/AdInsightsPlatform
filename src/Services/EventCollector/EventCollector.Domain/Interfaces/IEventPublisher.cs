using AdInsightsPlatform.Contracts.Events;

namespace EventCollector.Domain.Interfaces;

/// <summary>
/// Defines the contract for publishing domain events to the message broker (Kafka).
/// Abstraction keeps the application layer decoupled from Confluent.Kafka specifics.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes a domain event to Kafka.
    /// The message key is set to <see cref="BaseEvent.TenantId"/> to ensure
    /// all events from the same tenant are ordered within the same partition.
    /// </summary>
    /// <typeparam name="TEvent">The event type, must derive from <see cref="BaseEvent"/>.</typeparam>
    /// <param name="event">The event to publish.</param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : BaseEvent;
}

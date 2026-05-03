using System.Text.Json;
using AdInsightsPlatform.Contracts.Constants;
using AdInsightsPlatform.Contracts.Events;
using Confluent.Kafka;
using EventCollector.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EventCollector.Infrastructure.Kafka;

/// <summary>
/// Kafka-backed implementation of <see cref="IEventPublisher"/>.
/// Publishes all events to the "raw-events" topic using:
///   - Message Key = TenantId (ensures ordering per tenant within a partition)
///   - Message Value = JSON-serialised event
///   - Header: event-type (for Flink stream routing without deserialising the value)
/// </summary>
public sealed class KafkaEventPublisher : IEventPublisher
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public KafkaEventPublisher(
        KafkaProducerFactory producerFactory,
        ILogger<KafkaEventPublisher> logger)
    {
        _producer = producerFactory.CreateProducer();
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : BaseEvent
    {
        var messageValue = JsonSerializer.Serialize(@event, JsonOptions);

        var message = new Message<string, string>
        {
            // TenantId as key guarantees partition affinity per tenant
            Key = @event.TenantId,
            Value = messageValue,
            Headers = new Headers
            {
                // Event-type header enables Flink to route without deserialising the full payload
                { "event-type", System.Text.Encoding.UTF8.GetBytes(@event.EventType) },
                { "event-id", System.Text.Encoding.UTF8.GetBytes(@event.EventId.ToString()) }
            }
        };

        try
        {
            var deliveryResult = await _producer.ProduceAsync(
                KafkaTopics.RawEvents,
                message,
                cancellationToken);

            _logger.LogDebug(
                "Event published: Type={EventType} Key={TenantId} Partition={Partition} Offset={Offset}",
                @event.EventType,
                @event.TenantId,
                deliveryResult.Partition.Value,
                deliveryResult.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish event Type={EventType} EventId={EventId}: {Error}",
                @event.EventType,
                @event.EventId,
                ex.Error.Reason);

            throw;
        }
    }
}

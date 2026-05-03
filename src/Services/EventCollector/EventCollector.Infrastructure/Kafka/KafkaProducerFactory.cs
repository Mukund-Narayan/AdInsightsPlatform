using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EventCollector.Infrastructure.Kafka;

/// <summary>
/// Creates a configured Confluent Kafka producer.
/// The producer is registered as a Singleton — Confluent's IProducer is thread-safe
/// and designed to be shared across threads.
/// </summary>
public sealed class KafkaProducerFactory : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private bool _disposed;

    public KafkaProducerFactory(
        IConfiguration configuration,
        ILogger<KafkaProducerFactory> logger)
    {
        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,                          // Wait for all in-sync replicas
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 1000,
            EnableIdempotence = true,                 // Exactly-once delivery semantics
            CompressionType = CompressionType.Snappy, // Fast compression for high throughput
            LingerMs = 5,                             // Small batching delay for throughput
            BatchSize = 65536                    // 64KB batch size
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
                logger.LogError("Kafka producer error: {Reason}", error.Reason))
            .SetLogHandler((_, log) =>
                logger.LogDebug("Kafka [{Level}] {Message}", log.Level, log.Message))
            .Build();

        logger.LogInformation("Kafka producer created. BootstrapServers={Servers}", bootstrapServers);
    }

    /// <summary>Returns the shared Kafka producer instance.</summary>
    public IProducer<string, string> CreateProducer() => _producer;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Flush any pending messages before disposing
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
        _disposed = true;
    }
}

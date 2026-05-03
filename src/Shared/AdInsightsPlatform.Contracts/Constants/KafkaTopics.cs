namespace AdInsightsPlatform.Contracts.Constants;

/// <summary>
/// Centralised constants for all Kafka topic names used across the platform.
/// Prevents magic strings and ensures consistency between producer and consumer.
/// </summary>
public static class KafkaTopics
{
    /// <summary>
    /// Primary ingestion topic for all raw events.
    /// Partitioned by TenantId for data locality and ordering guarantees per tenant.
    /// </summary>
    public const string RawEvents = "raw-events";

    /// <summary>Flink output topic for aggregated click metrics (1-minute windows).</summary>
    public const string ProcessedAdClicks = "processed.ad-clicks";

    /// <summary>Flink output topic for aggregated impression metrics (1-minute windows).</summary>
    public const string ProcessedAdImpressions = "processed.ad-impressions";

    /// <summary>Flink output topic for click-to-basket conversion events (CEP result).</summary>
    public const string ProcessedClickToBasket = "processed.click-to-basket";

    /// <summary>Dead letter queue for events that failed validation or processing.</summary>
    public const string DeadLetterQueue = "dlq.events";
}

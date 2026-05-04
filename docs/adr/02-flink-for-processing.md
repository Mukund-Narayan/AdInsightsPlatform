# Apache Flink for Stream Processing


## Context

The platform must perform the following real-time computations on the event stream:

1. **Click aggregation**: Count AdClick events per (tenantId, campaignId) in 1-minute tumbling windows
2. **Impression aggregation**: Same for AdImpression events
3. **Click-to-Basket correlation**: Detect when a user adds to cart within 30 minutes of clicking an ad (Complex Event Processing)
4. **Stateful session tracking**: Maintain per-user session state across thousands of events

The challenge is that user sessions can span millions of concurrent users, requiring persistent, fault-tolerant state management.

## Decision

We chose **Apache Flink** for stateful stream processing.

## Alternatives Considered

| Option | Stateful | CEP Support | Exactly-Once | Scale |
|---|---|---|---|---|
| **Apache Flink** | Yes (RocksDB) | Yes (native CEP) | Yes | Very High |
| Apache Spark Structured Streaming | Limited | Via libraries | Yes | High |
| Kafka Streams | Yes (in-memory) | Limited | Yes | Medium |
| AWS Lambda | No | No | At-least-once | High |
| Azure Stream Analytics | Limited | Limited | At-least-once | High |

## Rationale

1. **Exactly-once semantics**: Flink + Kafka delivers exactly-once end-to-end, critical for billing-grade metrics
2. **Native CEP**: Flink CEP library natively supports complex pattern matching (click → addToCart within 30 min) without custom state management
3. **RocksDB state backend**: Supports petabytes of state — can track millions of concurrent user sessions without heap pressure
4. **Event time processing**: Flink's watermark-based event time handles out-of-order events from mobile/offline scenarios
5. **Fault tolerance**: Checkpointing to S3/MinIO ensures full recovery from failures without data loss
6. **Tumbling windows**: Native support for 1-minute tumbling event-time windows — the core aggregation primitive

## Key Design Decisions

### Watermarks
30-second bounded out-of-orderness watermark balances latency vs. completeness:
- Allows events delayed up to 30 seconds (network lag, mobile sync)
- Triggers window computation 30s after window end

### State Backend
RocksDB chosen over in-memory HashMapStateBackend for production:
- Handles state larger than JVM heap
- Spills to disk when needed
- Checkpoints to S3 for durability

### Parallelism
4 parallel task slots (matching Kafka partition count) for optimal throughput without shuffles.

## Consequences

- **Positive**: True stateful processing, CEP, exactly-once, fault tolerant
- **Positive**: Flink Web UI provides real-time job monitoring and backpressure visibility
- **Negative**: Java/JVM ecosystem — separate from .NET services (mitigated by Docker)
- **Negative**: Operational complexity vs. managed alternatives
- **Negative**: Memory-intensive for large state — requires careful resource planning
- **Mitigation**: RocksDB backend moves state to disk, checkpointing handles failures

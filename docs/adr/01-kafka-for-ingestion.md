# Apache Kafka for Event Ingestion


## Context

The AdInsightsPlatform must ingest high-velocity event streams from multiple retailer websites simultaneously. 
During peak events, event rates can reach millions of events per minute across all tenants combined. 
The ingestion layer must handle this load with:

- **Sub-100ms latency** from event emission to persistence
- **Durability** — no events lost, even if downstream processing is temporarily unavailable
- **Ordering** — events from the same tenant must be processed in order
- **Multi-tenancy** — events from different tenants must be isolatable

## Decision

We chose **Apache Kafka 3.7 in KRaft mode** (no Zookeeper) as the primary event streaming backbone.

## Alternatives Considered

| Option | Throughput | Durability | Ordering | Complexity |
|---|---|---|---|---|
| **Apache Kafka** | Very High | Yes | Per-partition | Medium |
| AWS Kinesis | High | Yes | Per-shard | Low |
| RabbitMQ | Medium | Yes | Per-queue | Low |
| Azure Event Hubs | High | Yes | Per-partition | Low |
| HTTP/REST only | Low | Depends | No | Very Low |

## Rationale

1. **Throughput**: Kafka sustains millions of messages/second with horizontal scaling via partitions
2. **Durability**: Messages persisted to disk with configurable replication factor (RF=3 in production)
3. **Tenant ordering**: Kafka message key = `tenantId` ensures all events from a tenant land in the same partition, guaranteeing processing order
4. **Flink integration**: Kafka has native, mature Flink connector with exactly-once semantics
5. **Replay capability**: Kafka's log retention allows replaying events for recovery or backfill
6. **KRaft mode**: Eliminates Zookeeper dependency, simplifying operations and improving startup time

## Topic Design

```
raw-events                  → All raw events (partitioned by tenantId)
processed.ad-clicks         → Flink aggregation output
processed.ad-impressions    → Flink aggregation output
processed.click-to-basket   → Flink CEP output
dlq.events                  → Dead Letter Queue for failed events
```

## Consequences

- **Positive**: High throughput, ordering guarantees, replay, Flink integration
- **Positive**: KRaft mode eliminates Zookeeper operational overhead
- **Negative**: Kafka requires more infrastructure than managed alternatives (AWS Kinesis)
- **Negative**: Partition count is immutable after creation — must plan capacity upfront
- **Mitigation**: Use 4 partitions initially with headroom for growth; monitor consumer lag

## Configuration Decisions

- `acks=all`: Ensures all in-sync replicas acknowledge before producer confirms
- `enable.idempotence=true`: Exactly-once producer semantics
- `compression.type=snappy`: Fast compression for high throughput
- `replication.factor=3`: Three-copy redundancy in production

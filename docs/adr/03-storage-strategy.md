# Multi-Tier Storage Strategy


## Context

The API must serve two distinct query patterns:

1. **Real-time queries** (last 24h to 30 days): Sub-second latency required. High write rate from Flink (millions of counter updates per minute). Low cardinality per query — count for one campaign.

2. **Historical queries** (>30 days): Acceptable latency of 1-5 seconds. Large data volumes — months or years of data. Complex aggregations across time ranges.

No single database handles both patterns optimally.

## Decision

We use a **multi-tier storage strategy** with automatic routing:

| Tier | Technology | Use Case | Retention |
|---|---|---|---|
| **Hot Path** | Apache Cassandra 4.1 | ≤30 days, real-time | 30 days (TTL) |
| **Cold Path** | ClickHouse 24.3 | >30 days, historical | 2 years |
| **Cache** | Redis 7.2 | API response caching | 30s TTL |
| **Archive** | MinIO / S3 | Raw event replay | Indefinite |

The `HybridAdMetricsRepository` transparently routes queries based on the `TimePeriod` value object, merging results when a query spans both tiers.

## Rationale

### Cassandra (Hot Path)
- **Counter columns**: Atomic increment without read-modify-write — perfect for high-frequency metric updates from Flink
- **Write optimised**: LSM-tree structure handles millions of writes/second
- **TTL**: Automatic data expiry after 30 days — no maintenance queries needed
- **Partition key**: `(tenant_id, campaign_id, metric_type)` ensures even distribution and efficient range scans by time

### ClickHouse (Cold Path)
- **Columnar storage**: 10-100x faster than RDBMS for analytical aggregations (SUM, COUNT)
- **MergeTree engine**: Efficient range scans over time-ordered data
- **Compression**: 5-10x compression ratio reduces storage cost for cold data
- **Vectorised execution**: Processes billions of rows in seconds

### Redis (Cache Layer)
- **Sub-millisecond reads**: API response time dominated by cache HIT rate
- **30-second TTL**: Balances freshness vs. load on Cassandra for real-time metrics
- **5-minute TTL**: Historical queries cached longer (data doesn't change)
- **Graceful degradation**: Cache failures fall through to Cassandra/ClickHouse (CachingBehavior)

## HybridAdMetricsRepository Routing Logic

```
Query Period → Router Decision:
  ≤30 days from now  → Cassandra only
  >30 days old       → ClickHouse only
  Spans boundary     → Query both concurrently → Sum results
```

## Consequences

- **Positive**: Each database optimised for its access pattern
- **Positive**: Cassandra TTL eliminates archiving maintenance
- **Positive**: Redis absorbs 80-90% of read load in typical usage patterns
- **Negative**: Three databases to operate and monitor
- **Negative**: HybridRepository must correctly identify period boundaries
- **Negative**: Consistency window: Cassandra data is eventually consistent (30-60s lag from Flink)
- **Mitigation**: API response includes `isRealTime` flag so clients understand data freshness

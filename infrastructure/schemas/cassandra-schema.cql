-- ─────────────────────────────────────────────────────────────────────────────
-- AdInsights Cassandra Schema
-- Purpose:
--   Hot-path real-time metric serving for Kafka + Flink streaming pipeline
--
-- Design Notes:
--   • Cassandra is used ONLY for low-latency operational reads
--   • ClickHouse handles analytical / historical queries
--   • Tables are modeled by query pattern
--   • Counter tables are intentionally minimal because Cassandra counters
--     have strict limitations
--
-- Supported Query Pattern:
--
--   SELECT *
--   FROM campaign_metrics
--   WHERE tenant_id = ?
--     AND campaign_id = ?
--     AND metric_type = ?
--     AND bucket_start >= ?
--     AND bucket_start <= ?
--
-- ─────────────────────────────────────────────────────────────────────────────


-- ─── Keyspace ────────────────────────────────────────────────────────────────

CREATE KEYSPACE IF NOT EXISTS ad_insights
WITH replication = {
    'class': 'NetworkTopologyStrategy',
    'datacenter1': 1
}
AND durable_writes = true;


USE ad_insights;


-- ─── campaign_metrics ────────────────────────────────────────────────────────
-- Real-time per-minute aggregations written by Flink
--
-- Example:
--   tenant1 | campaignA | CLICKS | 10:01 => +1
--   tenant1 | campaignA | CLICKS | 10:01 => +1
--
-- Counter tables:
--   • support atomic increments
--   • cannot use TTL
--   • cannot use secondary indexes
--   • cannot mix regular columns
--
-- Reads are always scoped by:
--   tenant_id + campaign_id + metric_type
--

CREATE TABLE IF NOT EXISTS campaign_metrics (
    tenant_id       text,
    campaign_id     text,
    metric_type     text,
    bucket_start    timestamp,
    count           counter,

    PRIMARY KEY (
        (tenant_id, campaign_id, metric_type),
        bucket_start
    )
)
WITH CLUSTERING ORDER BY (bucket_start DESC)
AND comment = 'Per-minute real-time metric aggregations'
AND compaction = {
    'class': 'TimeWindowCompactionStrategy',
    'compaction_window_unit': 'HOURS',
    'compaction_window_size': 1
};


-- ─── campaign_totals ─────────────────────────────────────────────────────────
-- Running total counters for O(1) aggregate lookups
--
-- Example:
--   campaignA | CLICKS => 1,204,553
--
-- Used by APIs for quick dashboard summaries
--

CREATE TABLE IF NOT EXISTS campaign_totals (
    tenant_id       text,
    campaign_id     text,
    metric_type     text,
    total_count     counter,

    PRIMARY KEY (
        (tenant_id, campaign_id),
        metric_type
    )
)
WITH comment = 'Running totals per campaign';


-- ─── campaign_totals_metadata ────────────────────────────────────────────────
-- Separate metadata table because counter tables cannot contain
-- regular non-counter columns.
--
-- Stores metadata such as:
--   • last aggregation timestamp
--   • watermark tracking
--   • stream sync status
--

CREATE TABLE IF NOT EXISTS campaign_totals_metadata (
    tenant_id       text,
    campaign_id     text,
    metric_type     text,
    last_updated    timestamp,

    PRIMARY KEY (
        (tenant_id, campaign_id),
        metric_type
    )
)
WITH comment = 'Metadata for campaign totals';


-- ─────────────────────────────────────────────────────────────────────────────
-- Example Queries
-- ─────────────────────────────────────────────────────────────────────────────

-- Increment counter
-- UPDATE campaign_metrics
-- SET count = count + 1
-- WHERE tenant_id = 'tenant1'
--   AND campaign_id = 'campaignA'
--   AND metric_type = 'CLICKS'
--   AND bucket_start = '2026-05-11 10:01:00';


-- Read recent buckets
-- SELECT *
-- FROM campaign_metrics
-- WHERE tenant_id = 'tenant1'
--   AND campaign_id = 'campaignA'
--   AND metric_type = 'CLICKS'
--   AND bucket_start >= '2026-05-11 10:00:00'
--   AND bucket_start <= '2026-05-11 11:00:00';


-- Read total counts
-- SELECT *
-- FROM campaign_totals
-- WHERE tenant_id = 'tenant1'
--   AND campaign_id = 'campaignA';
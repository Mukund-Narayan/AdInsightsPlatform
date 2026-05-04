
-- ─── Keyspace ────────────────────────────────────────────────────────────────
-- NetworkTopologyStrategy: production; SimpleStrategy: local dev
CREATE KEYSPACE IF NOT EXISTS ad_insights
WITH replication = {
    'class': 'NetworkTopologyStrategy',
    'dc1': 1  -- Set to 3 in production for RF=3
}
AND durable_writes = true;

USE ad_insights;

-- ─── campaign_metrics ────────────────────────────────────────────────────────
-- Hot path table: stores per-minute metric aggregations written by Flink.
-- Counter columns support atomic increments without read-modify-write cycles.
-- TTL: 30 days — data older than this is served from ClickHouse.
--
-- Written by: Flink AdClickAggregator, ImpressionAggregator, ClickToBasketCorrelator
-- Read by:    CassandraAdMetricsRepository (queries with SUM aggregation)
CREATE TABLE IF NOT EXISTS campaign_metrics (
    tenant_id       text,
    campaign_id     text,
    metric_type     text,       -- 'CLICKS' | 'IMPRESSIONS' | 'CLICK_TO_BASKET'
    bucket_start    timestamp,  -- 1-minute bucket start time
    count           counter,    -- Atomic counter — incremented by Flink sinks
    PRIMARY KEY ((tenant_id, campaign_id, metric_type), bucket_start)
) WITH CLUSTERING ORDER BY (bucket_start DESC)
  AND default_time_to_live = 2592000  -- 30 days in seconds
  AND comment = 'Per-minute ad metric aggregations — hot path for real-time queries'
  AND compaction = {
    'class': 'TimeWindowCompactionStrategy',
    'compaction_window_unit': 'HOURS',
    'compaction_window_size': 1
  };

-- ─── campaign_totals ─────────────────────────────────────────────────────────
-- Running total per campaign+metric — enables O(1) total count lookup
-- without summing time buckets. Updated by Flink as a side-effect sink.
--
-- Used by: API for fast total-count responses on large date ranges
CREATE TABLE IF NOT EXISTS campaign_totals (
    tenant_id       text,
    campaign_id     text,
    metric_type     text,
    total_count     counter,
    last_updated    timestamp,
    PRIMARY KEY ((tenant_id, campaign_id), metric_type)
) WITH comment = 'Running totals per campaign — O(1) total count reads';

-- ─── Indexes ─────────────────────────────────────────────────────────────────
-- Secondary index on bucket_start allows non-key time range queries
-- (only needed if querying without full partition key specification)
-- Note: Use with care — materialized views preferred for high-throughput
CREATE INDEX IF NOT EXISTS idx_campaign_metrics_bucket
    ON campaign_metrics (bucket_start);

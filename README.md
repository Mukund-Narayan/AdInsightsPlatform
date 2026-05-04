# AdInsightsPlatform

> **Real-Time Ad Analytics Streaming Platform for Online Retail Networks**

A production-grade, multi-tenant real-time advertising analytics platform designed for retail networks (e.g., Amazon Ads, Walmart Connect, Flipkart Ads, Target Roundel). Ingests high-velocity customer interaction events, processes them with Apache Flink, and exposes insights via a clean REST API.

---

## 📐 Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│  Retailer Websites / Mobile Apps / SDKs                             │
└───────────────────────────┬─────────────────────────────────────────┘
                            │  HTTP events
                            ▼
                   ┌────────────────┐
                   │  API Gateway   │  (NGINX/KONG · TLS · Rate Limiting)
                   └───────┬────────┘
              ┌────────────┴─────────────┐
              ▼                          ▼
   ┌──────────────────┐       ┌───────────────────────┐
   │ EventCollector   │       │   AdInsights API       │
   │ .NET 9 API       │       │   .NET 9 Minimal API   │
   │ POST /events     │       │   GET /ad/{id}/clicks  │
   └────────┬─────────┘       └────────┬──────────────┘
            │                          │
            ▼                          │ Cache-aside (Redis 30s TTL)
   ┌────────────────┐                  │
   │  Apache Kafka  │                  │
   │  3.7 (KRaft)  │                  ├── Cassandra (hot ≤30d)
   │  4 partitions  │                  └── ClickHouse (cold >30d)
   └────────┬───────┘
            │ consume
            ▼
   ┌────────────────────────────────────┐
   │        Apache Flink 1.19           │
   │  ┌──────────────────────────────┐  │
   │  │ AdClick Aggregator           │  │  1-min tumbling windows
   │  │ Impression Aggregator        │  │  → Cassandra + ClickHouse
   │  │ ClickToBasket Correlator CEP │  │  30-min session windows
   │  └──────────────────────────────┘  │
   │  State: RocksDB · Checkpoints: S3  │
   └────────────────────────────────────┘
```

### Key Design Decisions

| Concern | Technology | Reasoning |
|---|---|---|
| Event ingestion | Apache Kafka 3.7 (KRaft) | Millions msg/sec, ordering per tenant, exactly-once with Flink |
| Stream processing | Apache Flink 1.19 | Stateful CEP, RocksDB state, exactly-once end-to-end |
| Hot storage | Apache Cassandra 4.1 | Counter tables, TTL, sub-ms reads for ≤30 day queries |
| Cold storage | ClickHouse 24.3 | Columnar, vectorised, 100x faster aggregations over historical data |
| API caching | Redis 7.2 | 30s TTL cuts 80-90% of DB load during high traffic |
| API framework | .NET 9 Minimal API | Clean Architecture, CQRS via MediatR, high throughput |
| Multi-tenancy | JWT `tenant_id` claim | Cryptographically verified, enforced at all data access layers |

---

## 🗂️ Repository Structure

```
AdInsightsPlatform/
├── src/
│   ├── Services/
│   │   ├── AdInsights/           ← Insights query API (Clean Architecture)
│   │   │   ├── AdInsights.Domain/
│   │   │   ├── AdInsights.Application/
│   │   │   ├── AdInsights.Infrastructure/
│   │   │   └── AdInsights.Api/
│   │   └── EventCollector/       ← Event ingestion API
│   │       ├── EventCollector.Domain/
│   │       ├── EventCollector.Application/
│   │       ├── EventCollector.Infrastructure/
│   │       └── EventCollector.Api/
│   └── Shared/
│       ├── AdInsightsPlatform.Contracts/   ← Kafka event schemas
│       └── AdInsightsPlatform.Common/      ← Guards, utilities
├── flink-jobs/
│   └── ad-insights-processor/ src_TODO 
├── infrastructure/
│   ├── docker/docker-compose.yml ← Full local dev stack
│   ├── k8s/helm/                 ← Kubernetes Helm charts
│   └── schemas/
│       ├── cassandra-schema.cql  ← Cassandra DDL
│       └── kafka-schemas/        ← Avro schemas
├── docs/
│   ├── architecture/             ← draw.io diagrams (system + deployment)
│   ├── api/openapi.yaml          ← OpenAPI 3.1 specification
│   └── adr/                      ← Architecture Decision Records
├── tests/
│   └── AdInsights.UnitTests/     ← xUnit + Moq + FluentAssertions
└── AdInsightsPlatform.sln
```

---

## 🚀 Getting Started (Local Development)

### Prerequisites

| Tool | Version | Purpose |
|---|---|---|
| Docker Desktop | ≥ 29.0 | All infrastructure containers |
| .NET SDK | 9.0 | API services |
| Java JDK | 17 (optional) | Build Flink jobs locally |

### 1. Start the full infrastructure stack

```bash
docker compose -f infrastructure/docker/docker-compose.yml up -d
```

Wait for all services to be healthy (~2 minutes):

```bash
docker compose -f infrastructure/docker/docker-compose.yml ps
```

**Service endpoints:**

| Service | URL |
|---|---|
| AdInsights API + Swagger | http://localhost:5001 |
| EventCollector API | http://localhost:5002 |
| Flink Web UI | http://localhost:8082 |
| MinIO Console | http://localhost:9011 |
| Schema Registry | http://localhost:8081 |

### 2. Initialise Cassandra schema

```bash
docker exec adinsights-cassandra cqlsh -f /docker-entrypoint-initdb.d/schema.cql
```

### 3. Test event ingestion

```bash
curl -X POST http://localhost:5002/api/v1/events \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: tenant-demo-001" \
  -d '{
    "eventType": "AdClick",
    "campaignId": "campaign-summer-sale",
    "adId": "ad-unit-001",
    "userId": "user-hashed-abc123",
    "productId": "prod-laptop-pro"
  }'
```

### 4. Query metrics

```bash
# Get clicks for last 24 hours
curl http://localhost:5001/api/v1/ad/campaign-summer-sale/clicks \
  -H "Authorization: Bearer <JWT>"

# Get impressions for a date range
curl "http://localhost:5001/api/v1/ad/campaign-summer-sale/impressions?from=2024-01-01T00:00:00Z&to=2024-01-31T23:59:59Z" \
  -H "Authorization: Bearer <JWT>"

# Get click-to-basket conversions
curl http://localhost:5001/api/v1/ad/campaign-summer-sale/clickToBasket \
  -H "Authorization: Bearer <JWT>"
```

---

## 🔌 API Reference

Full OpenAPI 3.1 specification: [`docs/api/openapi.yaml`](docs/api/openapi.yaml)

### Endpoints

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/v1/ad/{campaignId}/clicks` | Total ad clicks in a date range |
| `GET` | `/api/v1/ad/{campaignId}/impressions` | Total ad impressions in a date range |
| `GET` | `/api/v1/ad/{campaignId}/clickToBasket` | Click-to-basket conversions |
| `POST` | `/api/v1/events` | Ingest a single event |
| `POST` | `/api/v1/events/batch` | Ingest up to 100 events |
| `GET` | `/healthz` | Health check |
| `GET` | `/metrics` | Prometheus scrape endpoint |

### Authentication

All endpoints require a JWT Bearer token with a `tenant_id` claim:

```
Authorization: Bearer eyJ...
```

### Example Response

```json
GET /api/v1/ad/campaign-abc-123/clicks

{
  "campaignId": "campaign-abc-123",
  "metricType": "Clicks",
  "count": 15234,
  "from": "2024-01-15T00:00:00Z",
  "to": "2024-01-15T23:59:59Z",
  "isRealTime": true,
  "computedAt": "2024-01-15T12:30:00Z"
}
```

`isRealTime: true` means data was served from Cassandra (~30s lag).  
`isRealTime: false` means data was served from ClickHouse (historical).

---

## ⚙️ Data Flow

```
1. Retailer website fires event → EventCollector POST /events
2. EventCollector validates, enriches with tenantId, publishes to Kafka
   Topic: raw-events (key = tenantId for partition affinity)
3. Apache Flink reads raw-events:
   a. AdClickAggregator    → TumblingWindow(1min) → count → Cassandra + ClickHouse
   b. ImpressionAggregator → TumblingWindow(1min) → count → Cassandra + ClickHouse
   c. ClickToBasketCorrelator (CEP):
      Pattern: AdClick → AddToCart within 30 min (keyed by userId+campaignId)
      Output: CTB conversion event → Cassandra + ClickHouse
4. AdInsights API receives GET /ad/{id}/clicks
   a. CachingBehavior checks Redis → return on HIT (sub-ms)
   b. HybridRepository routes:
      - Period ≤ 30 days → Cassandra (counter table SUM)
      - Period > 30 days → ClickHouse (columnar SUM)
      - Spans both      → Concurrent query, results merged
   c. Cache result in Redis (30s TTL)
   d. Return AdMetricsResponse
```

---

## 🏗️ Clean Architecture

The **AdInsights** service follows Clean Architecture with strict dependency rules:

```
AdInsights.Api              ← Presentation layer (Minimal API endpoints)
    ↓ references
AdInsights.Application      ← Use cases (CQRS queries, MediatR handlers, DTOs)
    ↓ references
AdInsights.Domain           ← Business rules (entities, value objects, interfaces)
    ↑ implemented by
AdInsights.Infrastructure   ← Adapters (Cassandra, ClickHouse, Redis)
```

**Key Patterns:**
- **CQRS**: `GetAdClicksQuery` → `GetAdClicksQueryHandler` via `ISender`
- **Repository Pattern**: `IAdMetricsRepository` → `HybridAdMetricsRepository` (routes to Cassandra/ClickHouse)
- **Cache-Aside**: `CachingBehavior<TRequest, TResponse>` — MediatR pipeline behavior
- **Decorator**: `HybridAdMetricsRepository` decorates both concrete repositories
- **Value Object**: `TimePeriod` encapsulates hot-path vs cold-path routing logic

---

## 🔒 Multi-Tenancy

Every layer enforces tenant isolation:

| Layer | Enforcement |
|---|---|
| API Gateway | JWT validation + rate limiting per tenant |
| TenantResolutionMiddleware | Extracts `tenant_id` from JWT claim |
| Kafka | Message key = `tenantId` (partition affinity) |
| Flink | Keyed streams by tenantId |
| Cassandra | `tenant_id` in every partition key |
| ClickHouse | `tenant_id` in all queries |
| Redis | Cache keys prefixed with `tenantId` |

---

## 📊 Monitoring & Observability

| Signal | Tool | Details |
|---|---|---|
| **Metrics** | Prometheus + Grafana | API p99 latency, Kafka consumer lag, Flink throughput, cache hit rate |
| **Tracing** | Jaeger + OpenTelemetry | Distributed request tracing across API → DB |
| **Logging** | Serilog → ELK Stack | Structured JSON, searchable in Kibana |
| **Alerting** | AlertManager | SLO breach, Flink checkpoint failure, Cassandra node down |

### Key Metrics to Monitor

```
kafka_consumer_group_lag                   → Flink processing backlog
flink_taskmanager_job_task_numRecordsIn    → Event throughput
http_request_duration_seconds{p99}        → API latency SLO
redis_hit_rate                             → Cache effectiveness
cassandra_client_request_latency           → Storage latency
```

Start monitoring stack:
```bash
docker compose -f infrastructure/docker/docker-compose.monitoring.yml up -d
```

---

## 📈 Scalability

### Horizontal Scaling

| Component | Scaling Unit | Strategy |
|---|---|---|
| EventCollector API | Pod replicas (HPA) | Stateless — add pods |
| AdInsights API | Pod replicas (HPA) | Stateless — add pods |
| Kafka | Broker nodes + partitions | Add brokers, increase partitions |
| Flink | TaskManager pods | Add task slots, increase parallelism |
| Cassandra | Data nodes (RF=3) | Token-aware load balancing |
| ClickHouse | Shards + replicas | MergeTree distributed table |
| Redis | Sentinel → Cluster | Redis Cluster for >100GB dataset |

### Peak Traffic (Black Friday) Strategy

1. **Pre-scale**: Increase Kafka partitions, Flink parallelism 2 weeks before
2. **HPA triggers**: CPU > 60% → scale API pods within 30 seconds
3. **Backpressure**: Flink watermark delay ensures no data loss during spikes
4. **Redis absorbs spikes**: 30s cache TTL means Cassandra sees 1 QPS per campaign regardless of API traffic

---

## ⚖️ Trade-offs & Challenges

| Challenge | Decision | Trade-off |
|---|---|---|
| Real-time vs accuracy | Cassandra (30s eventual consistency) | Slight delay vs. sub-ms reads |
| Flink CTB window | 30-minute attribution window | Missed conversions if delay >30 min |
| Storage cost | Hot/cold tiering | 3 databases to operate |
| Exactly-once | Kafka + Flink checkpointing | Higher memory, ~10% throughput overhead |
| Multi-tenancy | Shared infrastructure | Noisy-neighbour risk (mitigated with quotas) |

### Cost Monitoring

- **Cassandra**: Charge based on node count — TTL auto-deletes old data (no extra storage cost)
- **ClickHouse**: Charge based on storage — compression (5-10x) significantly reduces bill
- **Flink**: Charge based on TaskManager CPU/memory — scale down off-peak
- **Kafka**: Charge based on data throughput — Snappy compression reduces ~60% of payload size

---

## 🏃 Running Unit Tests

```bash
dotnet test tests/AdInsights.UnitTests/AdInsights.UnitTests.csproj --verbosity normal
```

Test coverage:
- `TimePeriodTests` — hot/cold path routing logic (7 tests)
- `GetAdClicksQueryHandlerTests` — query handler behavior (6 tests)
- `GetClickToBasketQueryHandlerTests` — CTB handler behavior (3 tests)

---

## 📄 Architecture Diagrams

Open in [draw.io](https://app.diagrams.net/):

- **System Architecture**: [`docs/architecture/system-architecture.drawio`](docs/architecture/system-architecture.drawio)
- **Deployment Diagram**: [`docs/architecture/deployment-diagram.drawio`](docs/architecture/deployment-diagram.drawio)

---

## 📋 Architecture Decision Records

| ADR | Decision | Status |
|---|---|---|
| [01](docs/adr/01-kafka-for-ingestion.md) | Apache Kafka for event ingestion | ✅ Accepted |
| [02](docs/adr/02-flink-for-processing.md) | Apache Flink for stream processing | ✅ Accepted |
| [03](docs/adr/03-storage-strategy.md) | Multi-tier storage (Cassandra + ClickHouse + Redis) | ✅ Accepted |

---

## 🛠️ Technology Stack

| Layer | Technology | Version |
|---|---|---|
| API Services | .NET 9 Minimal API | 9.0 |
| CQRS Mediator | MediatR | 12.x |
| Validation | FluentValidation | 11.x |
| Event Streaming | Apache Kafka (KRaft) | 3.7 |
| Schema Registry | Confluent Schema Registry | 7.6 |
| Stream Processing | Apache Flink | 1.19 |
| Hot Storage | Apache Cassandra | 4.1 |
| Cold Storage | ClickHouse | 24.3 |
| Cache | Redis | 7.2 |
| Object Storage | MinIO (S3-compatible) | Latest |
| Logging | Serilog → ELK Stack | 8.x |
| Tracing | OpenTelemetry + Jaeger | 1.9 |
| Metrics | Prometheus + Grafana | Latest |
| Auth | JWT Bearer (HS256) | - |
| Containers | Docker + Kubernetes | 29.x / 1.29 |
| Testing | xUnit + Moq + FluentAssertions | Latest |

---

## 📁 Key Files Reference

| File | Purpose |
|---|---|
| `docs/api/openapi.yaml` | OpenAPI 3.1 API specification |
| `docs/architecture/system-architecture.drawio` | System component diagram |
| `docs/architecture/deployment-diagram.drawio` | Kubernetes deployment diagram |
| `infrastructure/docker/docker-compose.yml` | Local development full stack |
| `infrastructure/schemas/cassandra-schema.cql` | Cassandra DDL |
| `infrastructure/schemas/kafka-schemas/ad-event-schema.avsc` | Kafka Avro schema |
| `src/Services/AdInsights/AdInsights.Domain/ValueObjects/TimePeriod.cs` | Hot/cold routing logic |
| `src/Services/AdInsights/AdInsights.Infrastructure/Persistence/Routing/HybridAdMetricsRepository.cs` | Storage router |
| `flink-jobs/ad-insights-processor/src/main/java/com/adinsights/processors/ClickToBasketCorrelator.java` | CEP conversion tracking |

---
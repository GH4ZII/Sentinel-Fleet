# Arkitektur

## Implementasjonsstatus

- [x] Modulær monolitt (ASP.NET Core)
- [x] PostgreSQL + PostGIS
- [x] RabbitMQ for telemetri
- [x] Redis
- [x] SignalR realtime
- [x] Python anomaly-service
- [x] Docker Compose-utviklingsmiljø
- [ ] Terraform / cloud deployment
- [ ] OpenTelemetry + Grafana/Prometheus-stack

### ADR-dokumentasjon (§37)

- [x] [ADR-001: Modular monolith](../adr/ADR-001-modular-monolith.md)
- [x] [ADR-002: PostgreSQL and PostGIS](../adr/ADR-002-postgresql-postgis.md)
- [x] [ADR-003: RabbitMQ for telemetry processing](../adr/ADR-003-rabbitmq-telemetry.md)
- [ ] ADR-004: SignalR for realtime updates
- [x] [ADR-005: Separate Python anomaly service](../adr/ADR-005-python-anomaly-service.md)
- [ ] ADR-006: Multi-tenant data isolation
- [x] [ADR-007: Rule-based detection before machine learning](../adr/ADR-007-rules-before-ml.md)
- [x] [ADR-008: Controlled AI tools with citations](../adr/ADR-008-controlled-ai-tools.md)
- [ ] ADR-009: OpenTelemetry observability
- [ ] ADR-010: Simulated telemetry for public demo

---

# 13. Teknisk arkitektur

```text
React + TypeScript
        │
        │ HTTPS / WebSocket
        ▼
ASP.NET Core API
        │
        ├── PostgreSQL + PostGIS
        ├── Redis
        ├── RabbitMQ
        ├── Background Workers
        ├── Object Storage
        └── Python Anomaly Service
```

---

# 14. Arkitekturvalg

## Modulær monolitt

Hovedapplikasjonen bygges som en modulær monolitt.

Fordeler:

* Enklere deployment
* Enklere lokal utvikling
* Tydelige domenegrenser
* Lavere kompleksitet enn mikrotjenester
* Mulighet for senere oppdeling

Python-tjenesten kan være separat fordi den har et annet teknologibehov.

## Hendelsesdrevet behandling

Telemetridata skal ikke behandles direkte i HTTP-requesten.

Foreslått flyt:

```text
Simulator
   │
   ▼
Telemetry ingestion API
   │
   ▼
RabbitMQ
   │
   ├── Telemetry processor
   ├── Rule engine
   ├── Anomaly service
   ├── Incident correlator
   └── Realtime publisher
```

Dette gjør systemet mer robust og skalerbart.

---

# 15. Backend-moduler

```text
Identity
Organizations
Users
Assets
Devices
Telemetry
Geofences
Rules
Detections
Incidents
RiskScoring
Relationships
AIAnalysis
Notifications
Reports
Audit
```

---

# 16. Teknologistack

## Frontend

* React
* TypeScript
* Vite
* Tailwind CSS
* TanStack Query
* React Router
* React Hook Form
* Zod
* MapLibre GL
* Recharts
* React Flow eller Cytoscape.js
* SignalR-client
* Playwright

## Backend

* ASP.NET Core
* C#
* Entity Framework Core
* PostgreSQL
* PostGIS
* SignalR
* RabbitMQ
* Redis
* OpenAPI
* FluentValidation
* Background workers
* OpenTelemetry

## Analyse

* Python
* FastAPI
* Pandas eller Polars
* NumPy
* scikit-learn
* Pydantic

## Infrastruktur

* Docker
* Docker Compose
* Terraform
* GitHub Actions
* Azure eller AWS
* Grafana
* Prometheus
* OpenTelemetry Collector
* S3-kompatibel objektlagring

## Testing

* xUnit
* Testcontainers
* Architecture tests
* Integration tests
* Vitest
* React Testing Library
* Playwright
* k6

---

---

# 30. Repository-struktur

```text
sentinel-fleet/
├── apps/
│   ├── api/
│   ├── web/
│   ├── simulator/
│   └── anomaly-service/
│
├── infrastructure/
│   ├── docker/
│   ├── terraform/
│   ├── monitoring/
│   └── scripts/
│
├── docs/
│   ├── architecture/
│   ├── adr/
│   ├── diagrams/
│   ├── api/
│   ├── testing/
│   └── performance/
│
├── .github/
│   └── workflows/
│
├── docker-compose.yml
├── .env.example
├── README.md
├── PROJECT_PLAN.md
└── CONTRIBUTING.md
```

## Backend-struktur

```text
apps/api/
├── src/
│   ├── SentinelFleet.Api/
│   ├── SentinelFleet.Application/
│   ├── SentinelFleet.Domain/
│   ├── SentinelFleet.Infrastructure/
│   └── SentinelFleet.Modules/
│       ├── Identity/
│       ├── Organizations/
│       ├── Assets/
│       ├── Devices/
│       ├── Telemetry/
│       ├── Geofences/
│       ├── Rules/
│       ├── Detections/
│       ├── Incidents/
│       ├── RiskScoring/
│       ├── AIAnalysis/
│       └── Audit/
│
└── tests/
    ├── UnitTests/
    ├── IntegrationTests/
    └── ArchitectureTests/
```

## Frontend-struktur

```text
apps/web/src/
├── app/
├── components/
├── features/
│   ├── auth/
│   ├── dashboard/
│   ├── live-map/
│   ├── assets/
│   ├── incidents/
│   ├── timeline/
│   ├── relationships/
│   ├── geofences/
│   ├── rules/
│   ├── simulator/
│   └── settings/
├── hooks/
├── layouts/
├── lib/
├── routes/
├── services/
├── types/
└── utils/
```

---

---

# 37. Tekniske beslutninger som skal dokumenteres

Opprett korte ADR-filer for:

```text
ADR-001: Modular monolith
ADR-002: PostgreSQL and PostGIS
ADR-003: RabbitMQ for telemetry processing
ADR-004: SignalR for realtime updates
ADR-005: Separate Python anomaly service
ADR-006: Multi-tenant data isolation
ADR-007: Rule-based detection before machine learning
ADR-008: Controlled AI tools with citations
ADR-009: OpenTelemetry observability
ADR-010: Simulated telemetry for public demo
```

Hver ADR skal inneholde:

* Problem
* Beslutning
* Alternativer
* Konsekvenser
* Begrunnelse

---

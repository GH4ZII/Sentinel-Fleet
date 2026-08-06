# Sentinel Fleet

Sentinel Fleet is an intelligent security and incident-analysis platform for companies that manage vehicles, machinery, trailers, tools, and other mobile equipment.

The platform receives real-time GPS and sensor data and uses it to detect suspicious activity such as:

* Vehicles moving outside working hours
* Geofence violations
* Unauthorized drivers
* Sudden GPS signal loss
* Abnormal fuel loss
* Odometer manipulation
* Unusual driving patterns
* Possible theft or misuse

When suspicious activity is detected, Sentinel Fleet combines related alerts into a single incident.

## Prerequisites

* Docker Desktop (or Docker Engine + Compose)
* [.NET 9 SDK](https://dotnet.microsoft.com/download) (for local API development)
* [Node.js 22+](https://nodejs.org/) (for local web development)

## Quick start

```bash
cp .env.example .env
docker compose up --build
```

| Service              | URL                          |
|----------------------|------------------------------|
| Web                  | http://localhost:3080        |
| API                  | http://localhost:8081        |
| API health           | http://localhost:8081/health |
| API readiness        | http://localhost:8081/health/ready |
| RabbitMQ management  | http://localhost:15673       |

Default RabbitMQ credentials: `sentinel` / `sentinel`.

Host ports are configured in `.env` (see `.env.example`) to avoid clashes with local installs.

## Local development (infra in Docker)

Start only infrastructure:

```bash
docker compose up postgres redis rabbitmq -d
```

### API

```bash
cd apps/api
dotnet run --project src/SentinelFleet.Api
```

Uses connection strings from `apps/api/src/SentinelFleet.Api/appsettings.Development.json`
(localhost ports matching `.env`: Postgres `5433`, Redis `6380`, RabbitMQ `5673`).

### Web

```bash
cd apps/web
npm install
npm run dev
```

Vite serves at http://localhost:5173 and proxies `/api`, `/health`, and `/hubs` to the API on port `8081`.

### Simulator (Week 3)

```bash
cd apps/simulator
python -m venv .venv
.venv\Scripts\activate          # Windows
pip install -r requirements.txt
python simulate.py              # creates 20 vehicles and streams GPS
```

Or with Docker: `docker compose --profile demo up simulator`

Open the web app, log in as the simulator user (default `simulator@sentinel.local` / `Simulator123!`), and watch the live map.

## Repository layout

```text
apps/api/          ASP.NET Core modular monolith
apps/web/          React + Vite + TypeScript frontend
apps/simulator/    Python GPS telemetry simulator
infrastructure/    Docker init scripts, Terraform, monitoring
docs/adr/          Architecture Decision Records
tests/             Unit, integration, and architecture tests
```

## Technologies

### Frontend

* React, TypeScript, Vite, Tailwind CSS, MapLibre, SignalR

### Backend

* ASP.NET Core, Entity Framework Core, PostgreSQL, PostGIS, RabbitMQ, Redis

### Infrastructure

* Docker, GitHub Actions, Terraform, OpenTelemetry, Grafana

See [Project_Plan.md](Project_Plan.md) for the full product and delivery plan.

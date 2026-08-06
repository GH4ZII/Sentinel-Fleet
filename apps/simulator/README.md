# Sentinel Fleet simulator

Python GPS simulator that creates >=20 vehicles and streams telemetry.

## Setup

```bash
cd apps/simulator
python -m venv .venv
# Windows
.venv\Scripts\activate
# macOS/Linux
# source .venv/bin/activate
pip install -r requirements.txt
```

## Run (API must be up)

```bash
# Default: http://localhost:8081, 20 vehicles, 3s interval
python simulate.py

# Bootstrap assets only
python simulate.py --bootstrap-only --count 20
```

Environment overrides: `API_BASE`, `SF_EMAIL`, `SF_PASSWORD`, `SF_ORG_NAME`, `SF_VEHICLE_COUNT`, `SF_INTERVAL_SEC`.

Device API keys are cached in `devices.json` (gitignored).

## Docker Compose

```bash
docker compose --profile demo up simulator
```

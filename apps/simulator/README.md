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
# Default: geofence_exit scenario + 20 vehicles
python simulate.py

# Bootstrap assets only
python simulate.py --bootstrap-only --count 20

# Random wander only (no geofence demo)
python simulate.py --scenario none
```

Environment overrides: `API_BASE`, `SF_EMAIL`, `SF_PASSWORD`, `SF_ORG_NAME`, `SF_VEHICLE_COUNT`, `SF_INTERVAL_SEC`, `SCENARIO`.

Device API keys are cached in `devices.json` (gitignored). The geofence exit scenario also writes `scenario.json`.

## Docker Compose

```bash
docker compose --profile demo up simulator
```

# Sentinel Fleet Anomaly Service

Python microservice that scores telemetry feature vectors for unusual usage patterns.

## Algorithms

1. **Heuristic (cold start)** – night / weekend movement rules when no baseline exists
2. **Statistical z-scores** – after a few samples of normal use
3. **Isolation Forest** – after ≥24 samples per asset

## API

| Method | Path | Description |
|--------|------|-------------|
| GET | `/health` | Liveness |
| POST | `/v1/score` | Score a feature vector |
| POST | `/v1/baseline/fit` | Fit / update baseline |
| GET | `/v1/baseline/{org}/{asset}` | Baseline status |

Response always includes `anomaly_score`, `confidence`, `model_version`, `features_used`, and `explanation`.

## Local run

```bash
cd apps/anomaly-service
python -m venv .venv
.venv\Scripts\activate   # Windows
pip install -r requirements.txt
uvicorn app.main:app --reload --port 8090
```

## Docker

Included in root `docker-compose.yml` as `anomaly-service`.

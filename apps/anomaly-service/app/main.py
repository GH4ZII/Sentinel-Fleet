from __future__ import annotations

import os
from pathlib import Path

from fastapi import FastAPI, HTTPException

from .detector import AnomalyDetector
from .models import (
    BaselineResponse,
    FitRequest,
    FitResponse,
    ScoreRequest,
    ScoreResponse,
)

MODEL_DIR = Path(os.environ.get("MODEL_DIR", "./data/models"))
detector = AnomalyDetector(MODEL_DIR)

app = FastAPI(
    title="Sentinel Fleet Anomaly Service",
    version="0.1.0",
    description="Baseline + Isolation Forest anomaly scoring for fleet telemetry features.",
)


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok", "service": "anomaly-service"}


@app.get("/health/live")
def health_live() -> dict[str, str]:
    return {"status": "alive"}


@app.post("/v1/score", response_model=ScoreResponse)
def score(request: ScoreRequest) -> ScoreResponse:
    return detector.score(request.organization_id, request.asset_id, request.features)


@app.post("/v1/baseline/fit", response_model=FitResponse)
def fit(request: FitRequest) -> FitResponse:
    if not request.samples:
        raise HTTPException(status_code=400, detail="At least one sample is required.")

    baseline = detector.fit(
        request.organization_id,
        request.asset_id,
        [s.features for s in request.samples],
    )
    return FitResponse(
        organization_id=request.organization_id,
        asset_id=request.asset_id,
        sample_count=len(baseline.samples),
        model_version="v1-iforest-stat",
        method=baseline.method,
        message="Baseline updated.",
    )


@app.get("/v1/baseline/{organization_id}/{asset_id}", response_model=BaselineResponse)
def get_baseline(organization_id: str, asset_id: str) -> BaselineResponse:
    baseline = detector.get_baseline(organization_id, asset_id)
    if baseline is None:
        return BaselineResponse(
            organization_id=organization_id,
            asset_id=asset_id,
            sample_count=0,
            model_version="v1-iforest-stat",
            method="none",
            ready=False,
        )
    return BaselineResponse(
        organization_id=organization_id,
        asset_id=asset_id,
        sample_count=len(baseline.samples),
        model_version="v1-iforest-stat",
        method=baseline.method,
        ready=len(baseline.samples) >= 5,
    )

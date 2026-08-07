from __future__ import annotations

from pydantic import BaseModel, Field


class FeatureVector(BaseModel):
    hour_of_day: float = Field(ge=0, le=23.999)
    day_of_week: float = Field(ge=0, le=6)
    speed_kph: float = Field(ge=0, default=0)
    ignition_on: float = Field(ge=0, le=1, default=0)
    fuel_level_percent: float | None = Field(default=None, ge=0, le=100)
    odometer_km: float | None = Field(default=None, ge=0)


class ScoreRequest(BaseModel):
    organization_id: str
    asset_id: str
    event_id: str | None = None
    recorded_at: str | None = None
    features: FeatureVector


class ScoreResponse(BaseModel):
    anomaly_score: float
    confidence: float
    model_version: str
    features_used: list[str]
    explanation: str
    is_anomaly: bool
    method: str


class FitSample(BaseModel):
    features: FeatureVector


class FitRequest(BaseModel):
    organization_id: str
    asset_id: str
    samples: list[FitSample]


class FitResponse(BaseModel):
    organization_id: str
    asset_id: str
    sample_count: int
    model_version: str
    method: str
    message: str


class BaselineResponse(BaseModel):
    organization_id: str
    asset_id: str
    sample_count: int
    model_version: str
    method: str
    ready: bool

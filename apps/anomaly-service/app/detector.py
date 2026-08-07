from __future__ import annotations

import json
import math
import pickle
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

import numpy as np
from sklearn.ensemble import IsolationForest

from .models import FeatureVector, ScoreResponse

MODEL_VERSION = "v1-iforest-stat"
MIN_SAMPLES_FOR_IFOREST = 24
ANOMALY_THRESHOLD = 0.55


FEATURE_NAMES = [
    "hour_of_day",
    "day_of_week",
    "speed_kph",
    "ignition_on",
    "fuel_level_percent",
]


def vectorize(features: FeatureVector) -> np.ndarray:
    fuel = features.fuel_level_percent if features.fuel_level_percent is not None else 50.0
    return np.array(
        [
            features.hour_of_day,
            features.day_of_week,
            features.speed_kph,
            features.ignition_on,
            fuel,
        ],
        dtype=np.float64,
    )


@dataclass
class AssetBaseline:
    organization_id: str
    asset_id: str
    samples: list[list[float]] = field(default_factory=list)
    isolation_forest: IsolationForest | None = None
    mean: np.ndarray | None = None
    std: np.ndarray | None = None
    method: str = "statistical"

    def to_dict(self) -> dict[str, Any]:
        return {
            "organization_id": self.organization_id,
            "asset_id": self.asset_id,
            "samples": self.samples,
            "method": self.method,
            "mean": self.mean.tolist() if self.mean is not None else None,
            "std": self.std.tolist() if self.std is not None else None,
        }

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> AssetBaseline:
        baseline = cls(
            organization_id=data["organization_id"],
            asset_id=data["asset_id"],
            samples=data.get("samples", []),
            method=data.get("method", "statistical"),
        )
        if data.get("mean") is not None:
            baseline.mean = np.array(data["mean"], dtype=np.float64)
        if data.get("std") is not None:
            baseline.std = np.array(data["std"], dtype=np.float64)
        return baseline


class AnomalyDetector:
    def __init__(self, model_dir: Path) -> None:
        self.model_dir = model_dir
        self.model_dir.mkdir(parents=True, exist_ok=True)
        self._baselines: dict[str, AssetBaseline] = {}
        self._load_all()

    def _key(self, organization_id: str, asset_id: str) -> str:
        return f"{organization_id}:{asset_id}"

    def _path(self, organization_id: str, asset_id: str) -> Path:
        safe = f"{organization_id}_{asset_id}".replace("/", "_")
        return self.model_dir / f"{safe}.json"

    def _iforest_path(self, organization_id: str, asset_id: str) -> Path:
        safe = f"{organization_id}_{asset_id}".replace("/", "_")
        return self.model_dir / f"{safe}.iforest.pkl"

    def _load_all(self) -> None:
        for path in self.model_dir.glob("*.json"):
            try:
                data = json.loads(path.read_text(encoding="utf-8"))
                baseline = AssetBaseline.from_dict(data)
                key = self._key(baseline.organization_id, baseline.asset_id)
                iforest_path = self._iforest_path(baseline.organization_id, baseline.asset_id)
                if iforest_path.exists():
                    with iforest_path.open("rb") as fh:
                        baseline.isolation_forest = pickle.load(fh)
                    baseline.method = "isolation_forest"
                self._baselines[key] = baseline
            except Exception:
                continue

    def _persist(self, baseline: AssetBaseline) -> None:
        path = self._path(baseline.organization_id, baseline.asset_id)
        path.write_text(json.dumps(baseline.to_dict()), encoding="utf-8")
        iforest_path = self._iforest_path(baseline.organization_id, baseline.asset_id)
        if baseline.isolation_forest is not None:
            with iforest_path.open("wb") as fh:
                pickle.dump(baseline.isolation_forest, fh)
        elif iforest_path.exists():
            iforest_path.unlink()

    def get_baseline(self, organization_id: str, asset_id: str) -> AssetBaseline | None:
        return self._baselines.get(self._key(organization_id, asset_id))

    def fit(self, organization_id: str, asset_id: str, samples: list[FeatureVector]) -> AssetBaseline:
        key = self._key(organization_id, asset_id)
        baseline = self._baselines.get(key) or AssetBaseline(organization_id, asset_id)
        for sample in samples:
            baseline.samples.append(vectorize(sample).tolist())

        # Cap history to keep models lean.
        if len(baseline.samples) > 2000:
            baseline.samples = baseline.samples[-2000:]

        matrix = np.array(baseline.samples, dtype=np.float64)
        baseline.mean = matrix.mean(axis=0)
        baseline.std = matrix.std(axis=0)
        baseline.std = np.where(baseline.std < 1e-6, 1.0, baseline.std)

        if len(baseline.samples) >= MIN_SAMPLES_FOR_IFOREST:
            model = IsolationForest(
                n_estimators=100,
                contamination=0.08,
                random_state=42,
            )
            model.fit(matrix)
            baseline.isolation_forest = model
            baseline.method = "isolation_forest"
        else:
            baseline.isolation_forest = None
            baseline.method = "statistical"

        self._baselines[key] = baseline
        self._persist(baseline)
        return baseline

    def score(self, organization_id: str, asset_id: str, features: FeatureVector) -> ScoreResponse:
        key = self._key(organization_id, asset_id)
        vector = vectorize(features)
        baseline = self._baselines.get(key)

        if baseline is None or len(baseline.samples) < 5:
            # Cold start: use domain heuristics for unusual hours + ignition.
            score, explanation = self._heuristic_score(features)
            is_anomaly = score >= ANOMALY_THRESHOLD
            return ScoreResponse(
                anomaly_score=round(score, 4),
                confidence=0.35,
                model_version=MODEL_VERSION,
                features_used=FEATURE_NAMES,
                explanation=explanation,
                is_anomaly=is_anomaly,
                method="heuristic",
            )

        if baseline.isolation_forest is not None:
            # decision_function: higher = more normal. Convert to [0,1] anomaly.
            raw = float(baseline.isolation_forest.decision_function(vector.reshape(1, -1))[0])
            # Typical range roughly [-0.5, 0.5]; map to 0..1 anomaly.
            score = 1.0 / (1.0 + math.exp(raw * 6.0))
            method = "isolation_forest"
            confidence = min(0.95, 0.55 + len(baseline.samples) / 400.0)
            explanation = self._explain_iforest(features, score, baseline)
        else:
            assert baseline.mean is not None and baseline.std is not None
            z = np.abs((vector - baseline.mean) / baseline.std)
            # Emphasize hour and ignition for usage anomalies.
            weights = np.array([1.4, 0.8, 0.9, 1.2, 0.6])
            weighted = float(np.average(z, weights=weights))
            score = min(1.0, weighted / 4.0)
            method = "statistical"
            confidence = min(0.8, 0.4 + len(baseline.samples) / 100.0)
            explanation = self._explain_statistical(features, z, score)

        # Online learning: fold observation into baseline lightly.
        baseline.samples.append(vector.tolist())
        if len(baseline.samples) % 20 == 0:
            self.fit(organization_id, asset_id, [])

        return ScoreResponse(
            anomaly_score=round(float(score), 4),
            confidence=round(float(confidence), 4),
            model_version=MODEL_VERSION,
            features_used=FEATURE_NAMES,
            explanation=explanation,
            is_anomaly=score >= ANOMALY_THRESHOLD,
            method=method,
        )

    def _heuristic_score(self, features: FeatureVector) -> tuple[float, str]:
        night = features.hour_of_day < 5 or features.hour_of_day >= 22
        moving = features.speed_kph > 5 and features.ignition_on >= 0.5
        if night and moving:
            return 0.78, (
                "Cold-start heuristic: movement with ignition during night hours "
                f"({features.hour_of_day:.0f}:00) without an established baseline."
            )
        if night and features.ignition_on >= 0.5:
            return 0.62, (
                "Cold-start heuristic: ignition during unusual hours "
                f"({features.hour_of_day:.0f}:00)."
            )
        if features.day_of_week >= 5 and moving:
            return 0.58, "Cold-start heuristic: weekend movement without baseline."
        return 0.18, "Cold-start heuristic: activity appears within expected daytime patterns."

    def _explain_iforest(self, features: FeatureVector, score: float, baseline: AssetBaseline) -> str:
        parts = [
            f"Isolation Forest scored {score:.2f} against {len(baseline.samples)} baseline samples."
        ]
        if features.hour_of_day < 5 or features.hour_of_day >= 22:
            parts.append(f"Hour {features.hour_of_day:.0f} is outside typical daytime use.")
        if features.ignition_on >= 0.5 and features.speed_kph > 5:
            parts.append(f"Vehicle moving at {features.speed_kph:.0f} km/h with ignition on.")
        return " ".join(parts)

    def _explain_statistical(self, features: FeatureVector, z: np.ndarray, score: float) -> str:
        worst_idx = int(np.argmax(z))
        name = FEATURE_NAMES[worst_idx]
        return (
            f"Statistical z-score anomaly {score:.2f}; highest deviation on '{name}' "
            f"(z={z[worst_idx]:.2f}). Observed hour={features.hour_of_day:.0f}, "
            f"speed={features.speed_kph:.0f}."
        )

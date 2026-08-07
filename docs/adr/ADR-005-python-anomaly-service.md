# ADR-005: Separate Python anomaly service

## Status

Accepted

## Problem

Anomaly detection benefits from scientific Python libraries (NumPy, scikit-learn) that are awkward to host inside the ASP.NET modular monolith.

## Decision

Run anomaly scoring as a separate FastAPI service (`apps/anomaly-service`) called over HTTP from the API after telemetry is persisted. The service returns score, confidence, model version, features used, and an explanation.

## Alternatives

* Embed ML.NET models in the API process
* Call cloud ML APIs for every telemetry event
* Defer all ML until a later microservice rewrite

## Consequences

* Clear language boundary and independent deploy/scale for ML
* Network dependency: API must tolerate anomaly-service downtime without blocking ingestion
* Baselines/models stored on a dedicated volume

## Rationale

Matches the project plan: keep the modular monolith simple while isolating the different technology need of Isolation Forest / statistical baselines.

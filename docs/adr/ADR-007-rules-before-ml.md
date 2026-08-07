# ADR-007: Rule-based detection before machine learning

## Status

Accepted

## Problem

Suspicious fleet activity can be expressed both as explicit policy rules and as statistical anomalies. Mixing them without priority creates opaque alerts.

## Decision

Evaluate deterministic rules first (geofence, work hours, GPS offline, unauthorized user, fuel loss). Anomaly scoring runs afterward as supporting evidence and may create `UsageAnomaly` detections, but ML output is never presented as an established fact in the analyst UI.

## Alternatives

* ML-only detection from day one
* Parallel competing detectors without correlation priority
* Human-only review with no automation

## Consequences

* Operators can explain every high-severity rule alert by configuration
* Anomaly scores enrich incidents and reports with citations to assessments
* False positives from cold-start models are contained by confidence and cooldowns

## Rationale

Week 4–5 delivered trustworthy rule detections; Week 6 adds ML as an additional signal without replacing the rule engine.

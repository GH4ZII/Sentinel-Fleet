# ADR-003: RabbitMQ for telemetry processing

## Status

Accepted

## Problem

Telemetry ingestion must stay fast and resilient. Processing rules, anomalies, and
incident correlation can be slower and should not block HTTP ingest requests.

## Decision

Accept telemetry over HTTP, publish messages to RabbitMQ, and process them asynchronously
in workers (rules, anomaly service, correlator, realtime publisher).

## Alternatives

* Process telemetry inline in the HTTP request
* Kafka / Azure Service Bus as the broker
* Database polling / LISTEN-NOTIFY only

## Consequences

* Clear ingest vs process separation
* Need broker health checks and dead-letter handling later
* Local Docker Compose must include RabbitMQ

## Rationale

RabbitMQ fits a modular monolith with background workers and is easy to run locally for week-by-week delivery.

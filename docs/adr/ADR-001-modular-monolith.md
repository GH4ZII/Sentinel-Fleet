# ADR-001: Modular monolith

## Status

Accepted

## Problem

Sentinel Fleet spans many domains (identity, assets, telemetry, detections, incidents, AI).
We need clear boundaries without the operational cost of many deployable services early on.

## Decision

Build the core platform as a modular monolith in ASP.NET Core.
Domain modules live as separate projects under `SentinelFleet.Modules.*` and are composed into one API host.

## Alternatives

* Microservices from day one
* Single project "big ball of mud"
* Vertically sliced folders without project boundaries

## Consequences

* One deployment unit for local Docker Compose and early CI
* Stronger compile-time isolation between modules
* Later extraction to services remains possible if a module needs independent scale

## Rationale

Matches the project plan: simpler local development, clear domain borders, lower complexity than microservices for the first eight weeks.

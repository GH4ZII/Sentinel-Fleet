# ADR-002: PostgreSQL and PostGIS

## Status

Accepted

## Problem

The platform must store relational multi-tenant data and answer geospatial questions
(geofence enter/exit, proximity, route reconstruction).

## Decision

Use PostgreSQL as the system of record and enable PostGIS for geometry/geography types and spatial queries.

## Alternatives

* Separate spatial database alongside a relational store
* MongoDB with GeoJSON indexes
* Application-side geofence evaluation only

## Consequences

* One database for business entities and spatial operations
* Requires PostGIS-enabled images/extensions in Docker and production
* EF Core + Npgsql is the primary data access path

## Rationale

PostGIS is mature for geofencing and fleet analytics, and keeps transactional and spatial data consistent.

# ADR-008: Controlled AI tools with citations

## Status

Accepted

## Problem

An incident analyst must summarize events without inventing facts or leaking cross-tenant data. A general chatbot with database access is unsafe.

## Decision

Provide a bounded incident analyst that may only call controlled tools (`get_incident`, timeline, risk, similar search, report generation, graph). Every factual claim includes a citation to system entities (detection, risk assessment, anomaly assessment, incident). Responses separate **facts**, **suspicions**, and **assumptions**. No direct SQL access.

## Alternatives

* Unrestricted LLM with raw schema access
* Static PDF report templates only
* Fully manual investigator notes

## Consequences

* Predictable, auditable analysis suitable for demo and compliance review
* First version is deterministic/tool-orchestrated (no external LLM key required)
* Tool usage is logged; report generation writes an audit entry

## Rationale

Matches the project plan requirement that the agent is not a general chatbot and must never present assumptions as facts.

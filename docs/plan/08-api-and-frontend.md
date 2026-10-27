# API og frontend

## Implementasjonsstatus

### API (§23)

- [x] Authentication
- [x] Organizations
- [x] Assets
- [x] Devices
- [x] Telemetry
- [x] Geofences
- [x] Rules
- [x] Incidents (+ timeline, relationships, comments, attachments, audit)
- [x] KI-analyse (kontrollerte tools)
- [ ] Dashboard-aggregater

### Frontend-sider (§24)

- [x] Innlogging / registrering (`/login`, `/register`)
- [x] Accept invitation (`/accept-invitation`)
- [ ] Dashboard (`/dashboard`)
- [x] Live-kart (på `/assets`, ikke egen `/live-map`)
- [x] Eiendeler (`/assets`, `/assets/new`, `/assets/:id`)
- [x] Devices (`/devices`, `/devices/new`, `/devices/:id`)
- [ ] Asset history / telemetry undersider
- [x] Hendelser (`/incidents`, detalj med faner)
- [x] Geofences
- [ ] Regler (`/rules`)
- [x] Administrasjon / settings (`/settings/organization`, `/settings/users`)

---

# 23. API-endpoints

Base path:

```text
/api/v1
```

## Authentication

```text
POST /api/v1/auth/register
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/logout
GET  /api/v1/auth/me
```

## Organizations

```text
GET   /api/v1/organizations/current
PATCH /api/v1/organizations/current
GET   /api/v1/organizations/current/members
POST  /api/v1/organizations/current/invitations
PATCH /api/v1/organizations/current/members/{memberId}
```

## Assets

```text
GET   /api/v1/assets
POST  /api/v1/assets
GET   /api/v1/assets/{assetId}
PATCH /api/v1/assets/{assetId}
GET   /api/v1/assets/{assetId}/current-status
GET   /api/v1/assets/{assetId}/telemetry
GET   /api/v1/assets/{assetId}/positions
GET   /api/v1/assets/{assetId}/incidents
```

## Devices

```text
GET   /api/v1/devices
POST  /api/v1/devices
GET   /api/v1/devices/{deviceId}
PATCH /api/v1/devices/{deviceId}
POST  /api/v1/devices/{deviceId}/rotate-key
```

## Telemetry

```text
POST /api/v1/telemetry/events
POST /api/v1/telemetry/batch
GET  /api/v1/telemetry/assets/{assetId}/latest
```

## Geofences

```text
GET    /api/v1/geofences
POST   /api/v1/geofences
GET    /api/v1/geofences/{geofenceId}
PATCH  /api/v1/geofences/{geofenceId}
DELETE /api/v1/geofences/{geofenceId}
```

## Rules

```text
GET   /api/v1/rules
POST  /api/v1/rules
GET   /api/v1/rules/{ruleId}
PATCH /api/v1/rules/{ruleId}
POST  /api/v1/rules/{ruleId}/enable
POST  /api/v1/rules/{ruleId}/disable
```

## Incidents

```text
GET   /api/v1/incidents
GET   /api/v1/incidents/{incidentId}
PATCH /api/v1/incidents/{incidentId}
GET   /api/v1/incidents/{incidentId}/timeline
GET   /api/v1/incidents/{incidentId}/relationships
GET   /api/v1/incidents/{incidentId}/positions
POST  /api/v1/incidents/{incidentId}/comments
POST  /api/v1/incidents/{incidentId}/attachments
POST  /api/v1/incidents/{incidentId}/assign
POST  /api/v1/incidents/{incidentId}/resolve
```

## KI-analyse

```text
POST /api/v1/incidents/{incidentId}/analysis/summary
POST /api/v1/incidents/{incidentId}/analysis/explain-risk
POST /api/v1/incidents/{incidentId}/analysis/missing-data
POST /api/v1/incidents/{incidentId}/analysis/similar-incidents
POST /api/v1/incidents/{incidentId}/analysis/report
```

## Dashboard

```text
GET /api/v1/dashboard/summary
GET /api/v1/dashboard/live-assets
GET /api/v1/dashboard/incidents
GET /api/v1/dashboard/risk-distribution
GET /api/v1/dashboard/system-health
```

---

# 24. Frontend-sider

## Innlogging

```text
/login
/register
/accept-invitation
```

## Dashboard

```text
/dashboard
```

Dashboardet skal vise:

* Eiendeler online
* Eiendeler offline
* Aktive hendelser
* Kritiske hendelser
* Hendelser siste 24 timer
* Gjennomsnittlig risikoscore
* Hendelser etter type
* Hendelser over tid
* Eiendeler med høyest risiko
* Systemstatus

## Live-kart

```text
/live-map
```

Funksjoner:

* Live-posisjoner
* Statusmarkører
* Geofences
* Filtrering
* Eiendelsdetaljer
* Aktive hendelser
* Siste kontakt
* Rutehistorikk

## Eiendeler

```text
/assets
/assets/new
/assets/:assetId
/assets/:assetId/history
/assets/:assetId/telemetry
/assets/:assetId/incidents
```

## Hendelser

```text
/incidents
/incidents/:incidentId
```

Hendelsesdetaljer skal ha faner:

```text
Oversikt
Tidslinje
Kart og avspilling
Relasjoner
Sensordata
KI-analyse
Vedlegg
Audit-logg
```

## Geofences

```text
/geofences
/geofences/new
/geofences/:geofenceId
```

## Regler

```text
/rules
/rules/new
/rules/:ruleId
```

## Administrasjon

```text
/settings/organization
/settings/users
/settings/roles
/settings/devices
/settings/integrations
/settings/security
```

---

# Roadmap og leveranse

## Overordnet status

- [x] Milepæl 1 – login, registrer kjøretøy, live posisjon
- [x] Milepæl 2 – geofence → detection → incident i frontend
- [x] Milepæl 3 – avspilling + kildebelagt forklaring
- [x] Uke 1–6 (kjerneløype)
- [ ] Uke 7 – produksjonskvalitet
- [ ] Uke 8 – deployment og presentasjon

---

# 31. Åtteukersplan

## Uke 1 – Prosjektgrunnlag

**Status:** ferdig

Mål:

* Prosjektet starter lokalt
* Backend, frontend og database kommuniserer

Oppgaver:

- [x] Opprett repository
- [x] Opprett ASP.NET Core-løsning
- [x] Opprett React-applikasjon
- [x] Sett opp PostgreSQL
- [x] Aktiver PostGIS
- [x] Sett opp Redis
- [x] Sett opp RabbitMQ
- [x] Sett opp Docker Compose
- [x] Opprett health checks
- [x] Sett opp logging
- [x] Sett opp GitHub Actions
- [x] Opprett første arkitekturtester

Leveranse:

```text
docker compose up
```

skal starte hele utviklingsmiljøet.

---

## Uke 2 – Organisasjoner, brukere og eiendeler

**Status:** ferdig (devices-UI og org-admin-UI mangler)

Mål:

* Brukere kan logge inn
* Bedrifter har isolerte data
* Kjøretøy og maskiner kan registreres

Oppgaver:

- [x] Authentication
- [x] Organization
- [x] Membership
- [x] Roller
- [x] Asset
- [x] AssetType
- [x] Device (API)
- [x] Multi-tenant-filter
- [x] Asset-register
- [x] Asset-detaljside
- [x] Enkle kartmarkører
- [ ] Devices-UI
- [ ] Org-/brukeradministrasjon i frontend

Leveranse:

En bruker kan logge inn og registrere et kjøretøy.

---

## Uke 3 – Telemetri og simulator

**Status:** ferdig

Mål:

* Systemet mottar kontinuerlige data
* Kjøretøy beveger seg på kartet

Oppgaver:

- [x] Telemetry-kontrakt
- [x] Ingestion endpoint
- [x] RabbitMQ-publisering
- [x] Telemetry worker
- [x] Lagring av posisjoner
- [x] Simulator
- [x] Simulering av minst 20 kjøretøy
- [x] SignalR
- [x] Live-kart

Leveranse:

Simulerte kjøretøy beveger seg på kartet i sanntid.

---

## Uke 4 – Geofences og regelmotor

**Status:** ferdig (regler-UI mangler)

Mål:

* Systemet oppdager konkrete sikkerhetsbrudd

Oppgaver:

- [x] Opprette geofences på kart
- [x] PostGIS-spørringer
- [x] Geofence enter og exit
- [x] Regelmotor
- [x] Bruk utenfor arbeidstid
- [x] GPS offline
- [x] Uautorisert bruker
- [x] Drivstofftap
- [x] Detections
- [x] Realtime-varsler
- [ ] Regler-UI (`/rules`)

Leveranse:

Et simulert kjøretøy forlater et område og utløser en alarm.

---

## Uke 5 – Incidents og hendelsesrekonstruksjon

**Status:** ferdig

Mål:

* Flere alarmer samles i én etterforskningshendelse

Oppgaver:

- [x] Incident-modell
- [x] Hendelseskorrelasjon
- [x] Risikoscore
- [x] Hendelsesstatus
- [x] Tidslinje
- [x] Kartavspilling
- [x] Kommentarer
- [x] Vedlegg
- [x] Audit-logg

Leveranse:

Brukeren kan åpne en hendelse og se nøyaktig hva som skjedde.

---

## Uke 6 – Analyse og KI

**Status:** ferdig (deterministiske KI-tools, ikke ekstern LLM)

Mål:

* Systemet oppdager avvik
* KI-agenten kan analysere hendelser med kilder

Oppgaver:

- [x] Python anomaly service
- [x] Baseline for normal bruk
- [x] Isolation Forest eller statistiske regler
- [x] Anomaly score
- [x] Hendelsesanalytiker
- [x] Kontrollerte agentverktøy
- [x] Kildehenvisninger
- [x] Fakta, mistanke og antakelser
- [x] Rapportgenerering
- [x] Relasjonsgraf

Leveranse:

Agenten genererer en kildebelagt hendelsesrapport.

---

## Uke 7 – Produksjonskvalitet

**Status:** delvis startet (Serilog, health, ErrorBoundary)

Mål:

* Systemet er sikkert, testbart og observerbart

Oppgaver:

- [ ] OpenTelemetry
- [ ] Grafana
- [ ] Prometheus
- [ ] Distributed tracing
- [ ] Rate limiting
- [ ] Sikkerhetstesting
- [ ] Playwright
- [ ] Integration tests
- [ ] Organisasjonsisoleringstester
- [x] Feilhåndtering (ErrorBoundary m.m.)
- [x] Loading states (delvis)
- [ ] Mobiltilpasning

Leveranse:

Systemet har automatiserte tester og observability-dashboard.

---

## Uke 8 – Deployment og presentasjon

**Status:** ikke startet

Mål:

* Sentinel Fleet er tilgjengelig som offentlig demo

Oppgaver:

- [ ] Terraform
- [ ] Cloud deployment
- [ ] GitHub Actions deployment
- [ ] Seed-data
- [ ] 100 simulerte kjøretøy
- [ ] Demo-scenarioer
- [ ] Belastningstest
- [ ] Performance-rapport
- [x] README
- [ ] Arkitekturdiagram
- [x] ADR-dokumenter (delvis: 001, 002, 003, 005, 007, 008)
- [ ] Skjermbilder
- [ ] Demo-video

Leveranse:

En fungerende offentlig demo med komplett teknisk dokumentasjon.

---

# 32. Første milepæl

**Status:** ferdig

- [x] En bruker kan logge inn, registrere et kjøretøy og se kjøretøyets simulerte posisjon oppdateres i sanntid på et kart.

Første milepæl er:

> En bruker kan logge inn, registrere et kjøretøy og se kjøretøyets simulerte posisjon oppdateres i sanntid på et kart.

Ikke start med KI, relasjonsgraf eller avansert anomalideteksjon før denne flyten fungerer stabilt.

---

# 33. Andre milepæl

**Status:** ferdig

- [x] Et kjøretøy kan forlate et geofence, utløse en detection og automatisk opprette en incident som vises i frontend.

Andre milepæl er:

> Et kjøretøy kan forlate et geofence, utløse en detection og automatisk opprette en incident som vises i frontend.

Flyten skal være:

```text
Simulator
→ Telemetry API
→ RabbitMQ
→ Telemetry worker
→ PostGIS
→ Regelmotor
→ Detection
→ Incident
→ SignalR
→ Frontend
```

---

# 34. Tredje milepæl

**Status:** ferdig

- [x] Brukeren kan åpne en incident, spille av hendelsen på kartet og se en kildebelagt forklaring av hvorfor hendelsen ble vurdert som mistenkelig.

Tredje milepæl er:

> Brukeren kan åpne en incident, spille av hendelsen på kartet og se en kildebelagt forklaring av hvorfor hendelsen ble vurdert som mistenkelig.

---

# 35. Første oppgaver

Arbeid i denne rekkefølgen:

- [x] 1. Opprett repository og mappestruktur
- [x] 2. Opprett ASP.NET Core-løsning
- [x] 3. Opprett React-applikasjon
- [x] 4. Sett opp Docker Compose
- [x] 5. Sett opp PostgreSQL med PostGIS
- [x] 6. Sett opp RabbitMQ og Redis
- [x] 7. Lag health endpoints
- [x] 8. Implementer Organization og Membership
- [x] 9. Implementer authentication
- [x] 10. Implementer Asset og Device
- [x] 11. Lag telemetrikontrakt
- [x] 12. Lag telemetry ingestion endpoint
- [x] 13. Lag enkel simulator
- [x] 14. Vis én bil på MapLibre-kartet
- [x] 15. Oppdater bilens posisjon med SignalR
- [x] 16. Utvid simulatoren til flere kjøretøy
- [x] 17. Implementer første geofence-regel
- [x] 18. Opprett første Detection
- [x] 19. Opprett første Incident
- [x] 20. Bygg hendelsestidslinjen

---

# 36. Første vertikale funksjon

**Status:** ferdig

Den første komplette funksjonen skal være:

> Simuler ett kjøretøy og vis kjøretøyets posisjon i sanntid på kartet.

Flyt:

```text
Simulator
→ Telemetry endpoint
→ RabbitMQ
→ Worker
→ PostgreSQL
→ SignalR
→ React
→ MapLibre
```

Denne funksjonen skal inkludere:

- [x] Validering
- [x] Organisasjonsisolering
- [x] Feilhåndtering
- [x] Logging
- [ ] Integrationstest
- [x] Realtime-oppdatering
- [x] Loading state
- [x] Offline-status

---

# 38. Definition of Done

En funksjon er ikke ferdig før:

- [x] Backend validerer data
- [x] Autorisasjon er kontrollert
- [x] Organisasjonsisolering er kontrollert
- [x] Feil håndteres
- [x] Logger er strukturert
- [x] Kritiske regler er testet
- [ ] API-kontrakten er dokumentert
- [x] Frontend har loading state
- [x] Frontend har error state
- [x] Realtime-tilkoblingen håndterer reconnect
- [x] CI passerer
- [x] Funksjonen fungerer i Docker-miljøet

---

# 39. Ikke en del av første versjon

Følgende skal ikke prioriteres i den første åtteukersperioden:

* Serviceplanlegging
* Vedlikeholdsplaner
* Arbeidsordre
* Reservedelslager
* Fakturering
* Komplett ERP-funksjonalitet
* Mobilapp i App Store
* Ekte maskinvareintegrasjon
* Avansert videoanalyse
* Ansiktsgjenkjenning
* Full automatisk etterforskning
* Store egenutviklede språkmodeller
* Mikrotjenestearkitektur
* Kubernetes lokalt

Kubernetes kan dokumenteres som en fremtidig produksjonsmulighet, men er ikke nødvendig for første versjon.

---

# 42. Nåværende prioritet

**Status:** Uke 1–6 er levert. Neste fokus er Uke 7.

Prioriter nå:

```text
OpenTelemetry
Grafana / Prometheus
Distributed tracing
API rate limiting
Integration tests (Testcontainers)
Organisasjonsisoleringstester
Playwright E2E
UX-polish (feil, loading, mobil)
```

Deretter Uke 8:

```text
Terraform / cloud deployment
CI deploy
Seed-data og demo-scenarioer
100 simulerte kjøretøy
Belastningstest
Dokumentasjon, skjermbilder, demo-video
```

Gjenstående UI-gaps som kan tas underveis:

```text
Rules UI (/rules)
Devices UI
Dashboard
Org-/brukeradministrasjon
```

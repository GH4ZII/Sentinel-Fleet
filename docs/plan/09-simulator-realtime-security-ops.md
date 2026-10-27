# Simulator, realtime, sikkerhet og drift

## Implementasjonsstatus

- [x] Datasimulator (≥20 kjøretøy, geofence-scenario)
- [ ] 100 kjøretøy + kontrollpanel for manuelle scenarioer
- [x] SignalR (posisjoner, detections, incidents)
- [x] JWT-auth + refresh + enhets-API-nøkler
- [x] Organisasjonsisolering
- [ ] API rate limiting
- [ ] OpenTelemetry / Grafana / Prometheus
- [x] Enhetstester (delvis) + arkitekturtester + CI
- [ ] Integration tests (Testcontainers)
- [ ] Playwright E2E
- [ ] k6 belastningstest

---

# 25. Datasimulator

Simulatoren er en sentral del av prosjektet.

Den skal kunne simulere minst 100 kjøretøy.

Hvert kjøretøy skal ha:

* Fast identitet
* Startposisjon
* Normale arbeidstider
* Normale ruter
* Tilordnet bruker
* Drivstoff eller batterinivå
* Sensorstatus

Simulatoren skal generere:

* GPS-posisjoner
* Hastighet
* Tenning
* Kilometerstand
* Drivstoff
* Batteri
* Dørsensor
* Førerinformasjon
* GPS-status

## Normale scenarioer

* Kjøretøy følger vanlig rute
* Kjøretøy står parkert
* Sjåfør starter skift
* Kjøretøy returnerer til base
* Maskin arbeider innenfor geofence

## Mistenkelige scenarioer

* Tyveri om natten
* Uautorisert sjåfør
* GPS-jamming
* Drivstofftyveri
* Kjøretøy forlater område
* Duplisert enhetsidentitet
* Kilometerstand manipulert
* Maskin beveger seg fra verksted
* Uvanlig lang stopp
* Kjøretøy dukker opp på umulig posisjon

Scenarioene skal kunne startes manuelt fra et kontrollpanel.

Eksempel:

```text
Start scenario:
"Tyveri av Varebil 12"

Simulatoren utfører:
1. Tenning aktiveres
2. Kjøretøy begynner å bevege seg
3. Geofence forlates
4. GPS-signalet forsvinner
5. Kjøretøy dukker opp et annet sted
```

---

# 26. Realtime

SignalR skal brukes til:

* Nye GPS-posisjoner
* Endret eiendelsstatus
* Nye detections
* Nye incidents
* Oppdatert risikoscore
* Systemstatus
* Simulatorstatus

Frontend skal oppdatere kartet uten å laste siden på nytt.

---

# 27. Sikkerhet

Minimumskrav:

* OAuth/OIDC eller sikker JWT-autentisering
* Refresh token rotation
* Rollebasert autorisasjon
* Organisasjonsisolering
* API rate limiting
* Enhetsnøkler for telemetri
* Rotasjon av enhetsnøkler
* Kryptering i transport
* Validering av alle meldinger
* Audit-logg
* Begrensning på filopplasting
* Sikre secrets
* Ingen sensitive data i logger
* Kontroll av KI-verktøy
* Logging av KI-forespørsler
* Beskyttelse mot prompt injection fra dokumenter og kommentarer

Organisasjonsisolering skal testes automatisk.

En bruker i én organisasjon skal aldri kunne hente data fra en annen organisasjon.

---

# 28. Observability

OpenTelemetry skal brukes for:

* Traces
* Metrics
* Logs

Grafana-dashboardet skal vise:

* API-responstid
* Antall requests
* Antall feil
* Antall telemetrimeldinger
* Meldinger per sekund
* RabbitMQ-kølengde
* Behandlingstid
* Antall detections
* Antall incidents
* SignalR-forbindelser
* Python-tjenestens responstid
* Databaseytelse
* Systemressurser

Alle telemetrimeldinger skal kunne spores gjennom:

```text
Ingestion
→ Queue
→ Processing
→ Detection
→ Incident
→ Frontend update
```

---

# 29. Testingstrategi

## Enhetstester

Test:

* Risikoberegning
* Geofence-regler
* Arbeidstidsregler
* Drivstoffregler
* Korrelasjon
* Statusoverganger
* Autorisasjonsregler
* Normalisering av telemetri

## Integrasjonstester

Test:

* PostgreSQL og PostGIS
* RabbitMQ
* Telemetri-ingestion
* Regelmotor
* Incident-opprettelse
* Organisasjonsisolering
* Autentisering
* SignalR
* Python-tjenesten

Bruk Testcontainers.

## End-to-end-tester

Playwright skal teste:

1. Bruker logger inn
2. Live-kart åpnes
3. Simulator starter en hendelse
4. Kjøretøyet beveger seg
5. Geofence-alarm opprettes
6. Incident vises
7. Tidslinjen oppdateres
8. Brukeren åpner hendelsen
9. Brukeren kjører KI-analyse
10. Brukeren genererer rapport

## Belastningstest

k6 skal teste:

* Telemetry ingestion
* Samtidige kjøretøy
* Meldinger per sekund
* Live-brukere
* Hendelsesopprettelse
* API-responstid

Resultatet skal dokumenteres.

---

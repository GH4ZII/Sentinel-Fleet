# Demo og portfolio

## Implementasjonsstatus

- [x] Grunnleggende geofence-/tyveri-flyt kan demoes lokalt via simulator
- [ ] Fullt «Mulig tyveri»-hovedscenario med alle tidspunkter
- [ ] Offentlig demo
- [ ] Demo-video / skjermbilder
- [ ] Portfolio-diagrammer (arkitektur, dataflyt, deployment)

---

# 40. Demo-scenario

Demo-organisasjon:

```text
Nordic Equipment Security AS
```

Demo-lokasjoner:

```text
Lager A – Trondheim
Verksted B – Tiller
Anleggsområde C – Orkanger
```

Demo-eiendeler:

```text
Varebil 12
Varebil 18
Volvo EC220 gravemaskin
Toyota gaffeltruck
Tilhenger 07
Verktøykasse 42
```

## Hovedscenario: Mulig tyveri

```text
02:10 – Varebil 12 står parkert ved Lager A
02:13 – Tenningen aktiveres
02:13 – Ingen sjåfør er autorisert
02:14 – Kjøretøyet begynner å bevege seg
02:14 – Kjøretøyet forlater geofence
02:19 – GPS-signalet forsvinner
02:20 – Systemet oppretter en kritisk incident
02:22 – Døren åpnes
03:02 – GPS-signalet kommer tilbake
03:02 – Kjøretøyet befinner seg 18 kilometer unna
03:03 – Risikoscore beregnes til 86
```

Brukeren skal kunne:

1. Se alarmen dukke opp på dashboardet
2. Åpne incidenten
3. Spille av ruten
4. Se hendelsestidslinjen
5. Se involverte personer og eiendeler
6. Se forklaring av risikoscoren
7. Be KI-agenten oppsummere hendelsen
8. Generere en rapport

---

# 41. Portfolio-resultat

Når prosjektet er ferdig, skal du kunne si:

> Jeg utviklet Sentinel Fleet, en multi-tenant sikkerhets- og analyseplattform som behandler GPS- og sensordata i sanntid. Plattformen oppdager mistenkelig bruk av kjøretøy og maskiner, korrelerer alarmer til hendelser, beregner risiko og rekonstruerer hendelser gjennom kartavspilling, tidslinjer og relasjonsgrafer. Systemet ble bygget med ASP.NET Core, React, PostgreSQL/PostGIS, RabbitMQ, Python, SignalR, Docker, Terraform og OpenTelemetry.

Prosjektet skal ha:

* Offentlig demo
* Automatisk datasimulator
* Profesjonell README
* Arkitekturdiagram
* Dataflytdiagram
* Deployment-diagram
* ADR-dokumenter
* API-dokumentasjon
* Testresultater
* Belastningstest
* Grafana-dashboard
* Demo-video
* Skjermbilder

---

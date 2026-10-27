# UX og KI-analytiker

## Implementasjonsstatus

- [x] Live-kart med sanntidsposisjoner (SignalR)
- [x] Hendelsesavspilling på kart
- [x] Visuell hendelsestidslinje
- [x] Relasjonsgraf
- [x] KI-analytiker med kontrollerte verktøy og kildehenvisninger (deterministisk, ADR-008)
- [ ] Dedikert dashboard-side
- [ ] Dedikert `/live-map`-rute (live-kart ligger på assets)

---

# 11. Visuelle hovedfunksjoner

## 11.1 Live-kart

Kartet skal vise alle aktive eiendeler.

Status:

```text
Grønn  – Normal
Gul    – Uvanlig aktivitet
Rød    – Kritisk hendelse
Grå    – Ingen kontakt
```

Kartet skal støtte:

* Live-posisjoner
* Markører
* Clustering
* Geofences
* Historisk rute
* Hendelsesmarkører
* Filtrering
* Valg av eiendel
* Statusoppdateringer i sanntid

Når brukeren velger en eiendel, skal systemet vise:

* Navn
* Type
* Nåværende status
* Hastighet
* Bruker
* Siste kontakt
* Aktive hendelser
* Risikonivå

---

## 11.2 Hendelsesavspilling

Brukeren skal kunne spille av en hendelse som en animasjon.

Kontroller:

* Spill av
* Pause
* Hastighet
* Gå til starten
* Gå til neste hendelse
* Velg tidspunkt

Under avspillingen skal kartet vise:

* Kjøretøyets posisjon
* Ruten
* Aktive alarmer
* Endringer i risikoscore
* Tidspunkt
* Sensorverdier

---

## 11.3 Visuell hendelsestidslinje

Tidslinjen skal skille mellom:

* Fakta
* Automatiske detections
* Brukerkommentarer
* KI-genererte analyser
* Vedlegg
* Systemhandlinger

Brukeren skal kunne filtrere tidslinjen etter:

* GPS
* Sensor
* Alarm
* Person
* Kommentar
* Bilde
* Dokument
* System

---

## 11.4 Relasjonsgraf

Systemet skal vise forbindelser mellom:

* Personer
* Kjøretøy
* Maskiner
* Sensorer
* Lokasjoner
* Geofences
* Hendelser
* Dokumenter
* Utstyr

Eksempel:

```text
Ansatt: Ola Nordmann
 ├── brukte → Varebil 12
 ├── hadde tilgang til → Lager A
 ├── var på skift → 08:00–16:00
 └── var involvert i → Hendelse 1042

Varebil 12
 ├── befant seg ved → Lager A
 ├── fraktet → Verktøykasse 42
 ├── besøkte → Adresse X
 ├── mistet GPS-signal ved → Adresse Y
 └── var involvert i → Hendelse 1042
```

Grafen skal støtte:

* Zoom
* Flytting av noder
* Valg av node
* Filtrering på relasjonstype
* Navigering til relatert objekt
* Begrensning av antall nivåer

---

# 12. KI-basert hendelsesanalytiker

Sentinel Fleet skal ha en avgrenset KI-agent.

Agenten skal ikke være en generell chatbot.

Den skal kun kunne arbeide med hendelser og data som brukeren har tilgang til.

## Agenten skal kunne

* Oppsummere en hendelse
* Forklare hvorfor en alarm ble utløst
* Forklare risikoscoren
* Finne relevante tidligere hendelser
* Sammenligne hendelser
* Foreslå hvilke data som mangler
* Generere en etterforskningsrapport
* Skille mellom fakta, mistanke og antakelser
* Vise kildehenvisninger til konkrete systemdata

## Eksempel på svar

```text
Varebil 12 forlot geofence Lager A klokken 02:14.

Det var ingen godkjent arbeidsaktivitet registrert, og den tilordnede
sjåføren hadde ikke aktivt skift. GPS-signalet forsvant fem minutter
senere mens tenningen fortsatt var aktiv.

Hendelsen har fått risikonivå 86 av 100.

Fakta:
- Tenning aktivert 02:13
- Geofence forlatt 02:14
- GPS-signal mistet 02:19
- Ingen autorisert bruker registrert

Mistanke:
- Kjøretøyet kan ha blitt brukt uten tillatelse

Manglende data:
- Video fra Lager A
- Adgangslogg fra porten
- Bekreftelse fra ansvarlig sjåfør
```

## Verktøy agenten kan bruke

Agenten skal få kontrollerte verktøy som:

```text
get_incident
get_incident_timeline
get_asset
get_asset_positions
get_sensor_readings
get_related_people
get_geofence_events
search_similar_incidents
get_user_shift
calculate_incident_risk
generate_incident_report
```

Agenten skal ikke få direkte tilgang til databasen.

Alle verktøy skal:

* Kontrollere organisasjon
* Kontrollere brukerens tilgang
* Returnere strukturerte data
* Logge bruken
* Ha tydelige input- og output-kontrakter

## Kildehenvisninger

Alle KI-genererte påstander skal referere til systemdata.

Eksempel:

```text
GPS-signalet forsvant klokken 02:19.
Kilde: Telemetry event TEL-88342

Kjøretøyet forlot geofence Lager A klokken 02:14.
Kilde: Detection DET-1821
```

Agenten skal aldri presentere antakelser som fakta.

---

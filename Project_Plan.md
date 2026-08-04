# Sentinel Fleet

> En intelligent sikkerhets- og analyseplattform som oppdager, analyserer og rekonstruerer mistenkelig aktivitet knyttet til kjøretøy, maskiner og mobilt utstyr.

---

# 1. Prosjektoversikt

Sentinel Fleet er en multi-tenant plattform for bedrifter som eier eller administrerer:

* Personbiler
* Varebiler
* Lastebiler
* Anleggsmaskiner
* Tilhengere
* Verktøy
* Mobilt utstyr
* Ansatte og sjåfører

Plattformen mottar en kontinuerlig strøm av posisjonsdata, kjøretøydata og sensordata.

Systemet analyserer dataene i sanntid for å oppdage:

* Mulig tyveri
* Uautorisert bruk
* Manipulasjon av sensordata
* Uvanlige bevegelser
* Brudd på geofences
* Mistenkelig drivstofforbruk
* Manglende GPS-signal
* Feil eller duplisert kjøretøyidentitet
* Bruk utenfor arbeidstid
* Bruk av feil person

Når en hendelse oppdages, rekonstruerer Sentinel Fleet hva som skjedde før, under og etter hendelsen.

---

# 2. Hovedidé

Sentinel Fleet skal ikke bare vise hvor kjøretøy befinner seg.

Systemet skal besvare spørsmål som:

* Hva skjedde?
* Når startet hendelsen?
* Hvilke kjøretøy, personer og steder var involvert?
* Hvilke regler eller modeller utløste alarmen?
* Hvor alvorlig er hendelsen?
* Hvilke fakta støtter konklusjonen?
* Hvilke data mangler?
* Har lignende hendelser skjedd tidligere?

Plattformen skal fungere som et digitalt etterforsknings- og sikkerhetssystem for mobile eiendeler.

---

# 3. Problem

Mange bedrifter har GPS-systemer som viser kjøretøy på et kart, men systemene gir ofte begrenset hjelp når noe mistenkelig skjer.

En administrator kan se at et kjøretøy har flyttet seg, men må selv finne ut:

* Hvem brukte kjøretøyet?
* Var personen på jobb?
* Var kjøretøyet godkjent for bruk?
* Forlot det et godkjent område?
* Forsvant GPS-signalet?
* Ble drivstoff tappet?
* Var det registrert et oppdrag?
* Har dette skjedd tidligere?
* Hvilke sensordata støtter mistanken?

Informasjonen kan være spredt mellom:

* GPS-systemer
* Adgangssystemer
* Arbeidsplaner
* Kjøretøyregistre
* Sensorlogger
* Bilder
* Dokumenter
* Meldinger
* Manuelle notater

Sentinel Fleet samler informasjonen og bygger én sammenhengende hendelse.

---

# 4. Mål

Prosjektet skal demonstrere kompetanse innen:

* Fullstack-utvikling
* Sanntidssystemer
* Hendelsesdrevet arkitektur
* Geografiske data
* Strømming av sensordata
* Anomalideteksjon
* Regelmotorer
* Kunstig intelligens
* Multi-tenancy
* Tilgangsstyring
* Observability
* Testing
* Infrastruktur som kode
* Sikkerhet
* Datavisualisering
* Systemarkitektur

---

# 5. Målgruppe

Sentinel Fleet kan brukes av:

* Transportbedrifter
* Entreprenører
* Bygg- og anleggsbedrifter
* Utleieselskaper
* Industribedrifter
* Vaktselskaper
* Kommunale driftsavdelinger
* Logistikkselskaper
* Bedrifter med kostbart mobilt utstyr

Typiske brukere:

* Driftsleder
* Sikkerhetsansvarlig
* Flåteadministrator
* Analytiker
* Sjåfør
* Maskinfører
* Systemadministrator

---

# 6. Kjernefunksjoner

## 6.1 Organisasjoner og brukere

Systemet skal støtte flere bedrifter.

Hver bedrift skal ha isolerte data og egne:

* Brukere
* Kjøretøy
* Maskiner
* Sensorer
* Geofences
* Hendelser
* Varslingsregler
* Rapporter

Foreslåtte roller:

### Organization Owner

Kan:

* Administrere organisasjonen
* Invitere brukere
* Endre roller
* Se alle hendelser
* Administrere sikkerhetsregler
* Administrere integrasjoner

### Security Manager

Kan:

* Se og undersøke hendelser
* Endre hendelsesstatus
* Legge til kommentarer
* Generere rapporter
* Administrere geofences
* Administrere regler

### Analyst

Kan:

* Se hendelser
* Bruke hendelsesanalytikeren
* Undersøke tidslinjer
* Analysere relasjoner
* Sammenligne tidligere hendelser

### Operator

Kan:

* Se tillatte kjøretøy
* Se egen aktivitet
* Registrere kommentarer
* Bekrefte eller avvise hendelser som gjelder dem

### Viewer

Kan:

* Se dashboard
* Se kjøretøy
* Se ferdigbehandlede hendelser
* Se rapporter

---

## 6.2 Register over mobile eiendeler

Systemet skal støtte forskjellige typer eiendeler:

* Kjøretøy
* Maskiner
* Tilhengere
* Verktøy
* Containere
* Annet mobilt utstyr

Hver eiendel kan ha:

* Unik identitet
* Navn
* Registreringsnummer
* Serienummer
* Produsent
* Modell
* Eiendelstype
* Status
* Nåværende lokasjon
* Tilordnet bruker
* Installerte sensorer
* Tillatte områder
* Tillatte arbeidstider

---

## 6.3 Datastrøm

Sentinel Fleet skal kunne motta:

* GPS-posisjon
* Hastighet
* Retning
* Kilometerstand
* Driftstimer
* Drivstoffnivå
* Batterinivå
* Tenning av og på
* Temperatur
* Vibrasjon
* Dør åpnet eller lukket
* Motorstatus
* Sensortilkobling
* Førers identitet
* Tidsstempel
* Enhetsidentitet

Første versjon skal bruke simulerte data.

Systemet skal likevel designes slik at ekte datakilder kan kobles til senere.

---

# 7. Hendelser systemet skal oppdage

## 7.1 Bruk utenfor arbeidstid

Oppstår når:

* Kjøretøyet startes utenfor tillatt arbeidstid
* Det ikke finnes et godkjent oppdrag
* Den registrerte brukeren ikke er på jobb

Eksempel:

```text
02:13 – Tenning aktivert
02:14 – Kjøretøy begynte å bevege seg
02:14 – Ingen registrert bruker på skift
02:15 – Alarm opprettet
```

---

## 7.2 Geofence-brudd

Oppstår når:

* En eiendel forlater et godkjent område
* En eiendel går inn i et forbudt område
* En eiendel ikke ankommer forventet område

Eksempel:

```text
02:14 – Varebil 12 forlot Lager A
02:15 – Ingen godkjent rute var registrert
02:16 – Risikoscore økte til 65
```

---

## 7.3 Tap av GPS-signal

Oppstår når:

* GPS-signalet plutselig stopper
* Kjøretøyet tidligere var i bevegelse
* Andre sensorer fortsatt sender data
* Signalbruddet skjer i forbindelse med en annen alarm

Eksempel:

```text
02:14 – Kjøretøy forlot geofence
02:19 – GPS-signalet forsvant
02:20 – Tenning var fortsatt aktiv
02:20 – Hendelsen ble oppgradert til kritisk
```

---

## 7.4 Kilometerstand går bakover

Oppstår når:

* Ny kilometerstand er lavere enn forrige avlesning
* Forskjellen ikke kan forklares av datakvalitet
* Kjøretøyidentiteten kan være feil eller manipulert

Mulige årsaker:

* Manipulert kilometerteller
* Feil sensor
* Duplisert enhetsidentitet
* Feil kjøretøy koblet til enheten

---

## 7.5 Unaturlig drivstofftap

Oppstår når:

* Drivstoffnivået synker raskt
* Kjøretøyet står stille
* Motoren er avslått
* Det ikke finnes normal forklaring

Mulige årsaker:

* Drivstofftyveri
* Lekkasje
* Sensorfeil

---

## 7.6 Uautorisert bruker

Oppstår når:

* En bruker logger inn i et kjøretøy vedkommende ikke har tilgang til
* Føreren ikke er på jobb
* Føreren bruker feil kjøretøy
* Føreren mangler nødvendig rolle eller tillatelse

---

## 7.7 Duplisert identitet

Oppstår når:

* To kjøretøy sender samme enhetsidentitet
* Samme kjøretøy rapporterer posisjoner som er fysisk umulige
* To samtidige datastrømmer hevder å komme fra samme sensor

---

## 7.8 Bevegelse under verkstedstatus

Oppstår når:

* Eiendelen er markert som utilgjengelig
* Eiendelen er registrert på verksted
* Sensorene rapporterer bevegelse
* Tenningen aktiveres uten godkjenning

---

## 7.9 Uvanlig kjøremønster

Oppstår når kjøretøyets bruk avviker fra normal historikk.

Eksempler:

* Uvanlig tidspunkt
* Uvanlig rute
* Uvanlig hastighet
* Uvanlig stoppested
* Mange korte stopp
* Stor avstand fra normale områder
* Rask akselerasjon eller bremsing
* Unormalt langt opphold på ukjent sted

---

## 7.10 Mulig tyveri eller misbruk

Dette er en sammensatt hendelse.

Eksempel:

```text
02:13 – Tenning aktivert utenfor arbeidstid
02:14 – Kjøretøy forlot godkjent område
02:14 – Registrert sjåfør var ikke på skift
02:19 – GPS-signalet forsvant
02:22 – Dørsensor rapporterte åpning
03:02 – Kjøretøyet sendte ny posisjon 18 km unna
```

Flere signaler kombineres til én samlet risikoscore.

---

# 8. Risikoscore

Alle hendelser skal få en risikoscore mellom 0 og 100.

Eksempel:

```text
0–29   Lav risiko
30–59  Moderat risiko
60–79  Høy risiko
80–100 Kritisk risiko
```

Risikoscoren kan beregnes fra:

* Hendelsestype
* Tidspunkt
* Eiendelens verdi eller kritikalitet
* Om brukeren var autorisert
* Geofence-brudd
* Tap av GPS-signal
* Tidligere lignende hendelser
* Sensorenes pålitelighet
* Antall samtidige alarmer
* Avstand fra normalt område

Eksempel:

```text
Bruk utenfor arbeidstid       +20
Ingen registrert sjåfør       +15
Geofence-brudd                +20
GPS-signal forsvant           +25
Kjøretøy med høy kritikalitet +10
Ingen arbeidsordre            +10

Total risikoscore: 100
```

Scoren skal lagre en forklaring, ikke bare et tall.

---

# 9. Hendelsesmodell

En rå alarm og en etterforskningshendelse er ikke det samme.

## Signal

En enkelt observasjon.

Eksempler:

* GPS-posisjon
* Lavt drivstoffnivå
* Dør åpnet
* Tenning aktivert
* GPS offline

## Detection

Et regel- eller modellresultat.

Eksempler:

* Geofence-brudd
* Uautorisert bruker
* Unaturlig drivstofftap
* Uvanlig rute

## Incident

En samlet hendelse som kan inneholde flere detections og signaler.

Eksempel:

```text
Incident: Mulig tyveri av Varebil 12

Detections:
- Bruk utenfor arbeidstid
- Uautorisert bruker
- Geofence-brudd
- GPS-signal forsvant
```

Dette gjør det mulig å samle relaterte alarmer i én sak.

---

# 10. Hendelsesrekonstruksjon

Systemet skal bygge en kronologisk tidslinje.

Eksempel:

```text
02:10 – Varebil 12 sto parkert ved Lager A
02:13 – Tenningen ble aktivert
02:13 – Ingen godkjent sjåfør var registrert
02:14 – Kjøretøyet begynte å bevege seg
02:14 – Kjøretøyet forlot geofence Lager A
02:19 – GPS-signalet forsvant
02:22 – Døren ble åpnet
03:02 – GPS-signalet kom tilbake
03:02 – Kjøretøyet befant seg 18 km unna
03:03 – Hendelsen fikk risikoscore 86
```

Tidslinjen kan inneholde:

* Posisjoner
* Sensorverdier
* Alarmer
* Personer
* Kommentarer
* Bilder
* Dokumenter
* Systemhandlinger
* Risikoscore-endringer
* KI-genererte observasjoner

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

# 13. Teknisk arkitektur

```text
React + TypeScript
        │
        │ HTTPS / WebSocket
        ▼
ASP.NET Core API
        │
        ├── PostgreSQL + PostGIS
        ├── Redis
        ├── RabbitMQ
        ├── Background Workers
        ├── Object Storage
        └── Python Anomaly Service
```

---

# 14. Arkitekturvalg

## Modulær monolitt

Hovedapplikasjonen bygges som en modulær monolitt.

Fordeler:

* Enklere deployment
* Enklere lokal utvikling
* Tydelige domenegrenser
* Lavere kompleksitet enn mikrotjenester
* Mulighet for senere oppdeling

Python-tjenesten kan være separat fordi den har et annet teknologibehov.

## Hendelsesdrevet behandling

Telemetridata skal ikke behandles direkte i HTTP-requesten.

Foreslått flyt:

```text
Simulator
   │
   ▼
Telemetry ingestion API
   │
   ▼
RabbitMQ
   │
   ├── Telemetry processor
   ├── Rule engine
   ├── Anomaly service
   ├── Incident correlator
   └── Realtime publisher
```

Dette gjør systemet mer robust og skalerbart.

---

# 15. Backend-moduler

```text
Identity
Organizations
Users
Assets
Devices
Telemetry
Geofences
Rules
Detections
Incidents
RiskScoring
Relationships
AIAnalysis
Notifications
Reports
Audit
```

---

# 16. Teknologistack

## Frontend

* React
* TypeScript
* Vite
* Tailwind CSS
* TanStack Query
* React Router
* React Hook Form
* Zod
* MapLibre GL
* Recharts
* React Flow eller Cytoscape.js
* SignalR-client
* Playwright

## Backend

* ASP.NET Core
* C#
* Entity Framework Core
* PostgreSQL
* PostGIS
* SignalR
* RabbitMQ
* Redis
* OpenAPI
* FluentValidation
* Background workers
* OpenTelemetry

## Analyse

* Python
* FastAPI
* Pandas eller Polars
* NumPy
* scikit-learn
* Pydantic

## Infrastruktur

* Docker
* Docker Compose
* Terraform
* GitHub Actions
* Azure eller AWS
* Grafana
* Prometheus
* OpenTelemetry Collector
* S3-kompatibel objektlagring

## Testing

* xUnit
* Testcontainers
* Architecture tests
* Integration tests
* Vitest
* React Testing Library
* Playwright
* k6

---

# 17. Datamodell

## Organization

```text
Id
Name
OrganizationNumber
CreatedAt
Settings
```

## User

```text
Id
Email
FirstName
LastName
PasswordHash
CreatedAt
LastLoginAt
```

## Membership

```text
Id
OrganizationId
UserId
Role
Status
CreatedAt
```

## Asset

```text
Id
OrganizationId
AssetTypeId
Name
AssetNumber
RegistrationNumber
SerialNumber
Manufacturer
Model
Status
Criticality
CurrentUserId
CreatedAt
UpdatedAt
```

## AssetType

```text
Id
OrganizationId
Name
Icon
Description
```

## Device

En fysisk eller simulert enhet som sender telemetri.

```text
Id
OrganizationId
AssetId
ExternalDeviceId
DeviceType
Status
LastSeenAt
FirmwareVersion
CreatedAt
```

## TelemetryEvent

```text
Id
OrganizationId
AssetId
DeviceId
EventType
RecordedAt
ReceivedAt
Latitude
Longitude
Speed
Heading
Odometer
FuelLevel
BatteryLevel
IgnitionOn
EngineHours
Temperature
Vibration
DoorOpen
DriverId
RawPayload
```

Ikke alle feltene må være satt for hver event.

## Geofence

```text
Id
OrganizationId
Name
Description
Geometry
GeofenceType
IsActive
CreatedAt
```

PostGIS skal brukes for geometri.

## AssetGeofence

```text
Id
OrganizationId
AssetId
GeofenceId
RuleType
ValidFrom
ValidTo
```

## DriverAssignment

```text
Id
OrganizationId
AssetId
UserId
ValidFrom
ValidTo
AssignmentType
```

## WorkShift

```text
Id
OrganizationId
UserId
StartsAt
EndsAt
Status
```

## DetectionRule

```text
Id
OrganizationId
Name
RuleType
Description
Configuration
Severity
IsActive
CreatedAt
UpdatedAt
```

Konfigurasjonen kan lagres som JSON.

## Detection

```text
Id
OrganizationId
AssetId
RuleId
DetectionType
Severity
Confidence
RiskContribution
Title
Description
TriggeredAt
SourceEventIds
Metadata
IncidentId
```

## Incident

```text
Id
OrganizationId
PrimaryAssetId
Title
Description
IncidentType
Status
Severity
RiskScore
Confidence
StartedAt
EndedAt
DetectedAt
AssignedToUserId
CreatedAt
UpdatedAt
```

## IncidentTimelineEntry

```text
Id
OrganizationId
IncidentId
EntryType
Timestamp
Title
Description
SourceType
SourceId
Latitude
Longitude
Metadata
CreatedByUserId
CreatedAt
```

## IncidentEntity

Kobler personer, eiendeler og steder til en hendelse.

```text
Id
OrganizationId
IncidentId
EntityType
EntityId
RelationshipType
FirstObservedAt
LastObservedAt
Metadata
```

## IncidentComment

```text
Id
OrganizationId
IncidentId
UserId
Content
CreatedAt
UpdatedAt
```

## IncidentAttachment

```text
Id
OrganizationId
IncidentId
UploadedByUserId
Name
ContentType
StorageKey
Size
CreatedAt
```

## RiskAssessment

```text
Id
OrganizationId
IncidentId
Score
RiskLevel
Factors
ModelVersion
CalculatedAt
```

## AIAnalysis

```text
Id
OrganizationId
IncidentId
RequestedByUserId
AnalysisType
PromptVersion
Model
Result
Sources
CreatedAt
```

## AuditLog

```text
Id
OrganizationId
UserId
Action
EntityType
EntityId
OldValues
NewValues
IpAddress
CreatedAt
```

---

# 18. Telemetri-format

Alle telemetrimeldinger bør bruke et felles format.

Eksempel:

```json
{
  "eventId": "01JABC123XYZ",
  "organizationId": "org-001",
  "deviceId": "device-vehicle-12",
  "assetId": "asset-vehicle-12",
  "recordedAt": "2026-08-04T00:14:00Z",
  "eventType": "position",
  "position": {
    "latitude": 63.4305,
    "longitude": 10.3951,
    "speedKph": 42.5,
    "heading": 215
  },
  "vehicle": {
    "ignitionOn": true,
    "odometerKm": 84512.4,
    "fuelLevelPercent": 68
  },
  "driver": {
    "userId": null
  }
}
```

Meldingene skal ha:

* Unik event-ID
* Organisasjon
* Enhet
* Eiendel
* Tidspunkt hos enheten
* Tidspunkt mottatt av serveren
* Versjon av meldingsformatet

---

# 19. Databehandlingsflyt

```text
1. Simulator sender telemetri
2. API validerer enhet og organisasjon
3. Meldingen publiseres til RabbitMQ
4. Telemetry worker normaliserer meldingen
5. Data lagres i PostgreSQL
6. Regelmotor evaluerer meldingen
7. Python-tjenesten beregner anomalier
8. Detections blir opprettet
9. Incident correlator samler relaterte detections
10. Risikoscore beregnes
11. SignalR sender oppdatering til frontend
12. Live-kart og dashboard oppdateres
```

---

# 20. Regelmotor

Første versjon skal være regelbasert.

Eksempel på regelkonfigurasjon:

```json
{
  "ruleType": "movement_outside_working_hours",
  "enabled": true,
  "allowedFrom": "06:00",
  "allowedTo": "22:00",
  "minimumSpeedKph": 5,
  "severity": "high",
  "riskContribution": 20
}
```

Regelmotoren skal støtte:

* Aktivering og deaktivering
* Organisasjonsspesifikke innstillinger
* Eiendelsspesifikke regler
* Alvorlighetsgrad
* Risikobidrag
* Regelversjon
* Forklaring av hvorfor regelen slo ut

---

# 21. Anomalideteksjon

Maskinlæring skal komme etter at de tydelige reglene fungerer.

Første modeller kan være:

## Unormal brukstid

Modellen lærer når et kjøretøy normalt brukes.

Input:

* Ukedag
* Klokkeslett
* Varighet
* Aktiv bruker
* Eiendel

## Unormal rute

Modellen sammenligner dagens rute med historiske ruter.

Input:

* Startsted
* Sluttsted
* Geografiske punkter
* Tidspunkt
* Ukedag

## Unormalt drivstofforbruk

Input:

* Drivstoffendring
* Kjørt avstand
* Motorstatus
* Hastighet
* Tid

## Mulige algoritmer

* Isolation Forest
* Local Outlier Factor
* One-Class SVM
* Clustering
* Statistiske terskler

Modellen skal returnere:

```text
Anomaly score
Confidence
Model version
Features used
Explanation
```

Maskinlæringsresultatet skal ikke automatisk bli presentert som et faktum.

---

# 22. Hendelseskorrelasjon

Flere detections skal kunne samles i én incident.

Eksempel:

```text
Detection 1: Bruk utenfor arbeidstid
Detection 2: Uautorisert bruker
Detection 3: Geofence-brudd
Detection 4: GPS offline
```

Korrelasjon kan baseres på:

* Samme eiendel
* Nært tidspunkt
* Samme bruker
* Samme lokasjon
* Samme hendelsestype
* Eksisterende åpen incident

Første versjon kan bruke en regel:

```text
Hvis detections gjelder samme eiendel
og skjer innenfor 30 minutter,
knyttes de til samme åpne incident.
```

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

# 30. Repository-struktur

```text
sentinel-fleet/
├── apps/
│   ├── api/
│   ├── web/
│   ├── simulator/
│   └── anomaly-service/
│
├── infrastructure/
│   ├── docker/
│   ├── terraform/
│   ├── monitoring/
│   └── scripts/
│
├── docs/
│   ├── architecture/
│   ├── adr/
│   ├── diagrams/
│   ├── api/
│   ├── testing/
│   └── performance/
│
├── .github/
│   └── workflows/
│
├── docker-compose.yml
├── .env.example
├── README.md
├── PROJECT_PLAN.md
└── CONTRIBUTING.md
```

## Backend-struktur

```text
apps/api/
├── src/
│   ├── SentinelFleet.Api/
│   ├── SentinelFleet.Application/
│   ├── SentinelFleet.Domain/
│   ├── SentinelFleet.Infrastructure/
│   └── SentinelFleet.Modules/
│       ├── Identity/
│       ├── Organizations/
│       ├── Assets/
│       ├── Devices/
│       ├── Telemetry/
│       ├── Geofences/
│       ├── Rules/
│       ├── Detections/
│       ├── Incidents/
│       ├── RiskScoring/
│       ├── AIAnalysis/
│       └── Audit/
│
└── tests/
    ├── UnitTests/
    ├── IntegrationTests/
    └── ArchitectureTests/
```

## Frontend-struktur

```text
apps/web/src/
├── app/
├── components/
├── features/
│   ├── auth/
│   ├── dashboard/
│   ├── live-map/
│   ├── assets/
│   ├── incidents/
│   ├── timeline/
│   ├── relationships/
│   ├── geofences/
│   ├── rules/
│   ├── simulator/
│   └── settings/
├── hooks/
├── layouts/
├── lib/
├── routes/
├── services/
├── types/
└── utils/
```

---

# 31. Åtteukersplan

## Uke 1 – Prosjektgrunnlag

Mål:

* Prosjektet starter lokalt
* Backend, frontend og database kommuniserer

Oppgaver:

* Opprett repository
* Opprett ASP.NET Core-løsning
* Opprett React-applikasjon
* Sett opp PostgreSQL
* Aktiver PostGIS
* Sett opp Redis
* Sett opp RabbitMQ
* Sett opp Docker Compose
* Opprett health checks
* Sett opp logging
* Sett opp GitHub Actions
* Opprett første arkitekturtester

Leveranse:

```text
docker compose up
```

skal starte hele utviklingsmiljøet.

---

## Uke 2 – Organisasjoner, brukere og eiendeler

Mål:

* Brukere kan logge inn
* Bedrifter har isolerte data
* Kjøretøy og maskiner kan registreres

Oppgaver:

* Authentication
* Organization
* Membership
* Roller
* Asset
* AssetType
* Device
* Multi-tenant-filter
* Asset-register
* Asset-detaljside
* Enkle kartmarkører

Leveranse:

En bruker kan logge inn og registrere et kjøretøy.

---

## Uke 3 – Telemetri og simulator

Mål:

* Systemet mottar kontinuerlige data
* Kjøretøy beveger seg på kartet

Oppgaver:

* Telemetry-kontrakt
* Ingestion endpoint
* RabbitMQ-publisering
* Telemetry worker
* Lagring av posisjoner
* Simulator
* Simulering av minst 20 kjøretøy
* SignalR
* Live-kart

Leveranse:

Simulerte kjøretøy beveger seg på kartet i sanntid.

---

## Uke 4 – Geofences og regelmotor

Mål:

* Systemet oppdager konkrete sikkerhetsbrudd

Oppgaver:

* Opprette geofences på kart
* PostGIS-spørringer
* Geofence enter og exit
* Regelmotor
* Bruk utenfor arbeidstid
* GPS offline
* Uautorisert bruker
* Drivstofftap
* Detections
* Realtime-varsler

Leveranse:

Et simulert kjøretøy forlater et område og utløser en alarm.

---

## Uke 5 – Incidents og hendelsesrekonstruksjon

Mål:

* Flere alarmer samles i én etterforskningshendelse

Oppgaver:

* Incident-modell
* Hendelseskorrelasjon
* Risikoscore
* Hendelsesstatus
* Tidslinje
* Kartavspilling
* Kommentarer
* Vedlegg
* Audit-logg

Leveranse:

Brukeren kan åpne en hendelse og se nøyaktig hva som skjedde.

---

## Uke 6 – Analyse og KI

Mål:

* Systemet oppdager avvik
* KI-agenten kan analysere hendelser med kilder

Oppgaver:

* Python anomaly service
* Baseline for normal bruk
* Isolation Forest eller statistiske regler
* Anomaly score
* Hendelsesanalytiker
* Kontrollerte agentverktøy
* Kildehenvisninger
* Fakta, mistanke og antakelser
* Rapportgenerering
* Relasjonsgraf

Leveranse:

Agenten genererer en kildebelagt hendelsesrapport.

---

## Uke 7 – Produksjonskvalitet

Mål:

* Systemet er sikkert, testbart og observerbart

Oppgaver:

* OpenTelemetry
* Grafana
* Prometheus
* Distributed tracing
* Rate limiting
* Sikkerhetstesting
* Playwright
* Integration tests
* Organisasjonsisoleringstester
* Feilhåndtering
* Loading states
* Mobiltilpasning

Leveranse:

Systemet har automatiserte tester og observability-dashboard.

---

## Uke 8 – Deployment og presentasjon

Mål:

* Sentinel Fleet er tilgjengelig som offentlig demo

Oppgaver:

* Terraform
* Cloud deployment
* GitHub Actions deployment
* Seed-data
* 100 simulerte kjøretøy
* Demo-scenarioer
* Belastningstest
* Performance-rapport
* README
* Arkitekturdiagram
* ADR-dokumenter
* Skjermbilder
* Demo-video

Leveranse:

En fungerende offentlig demo med komplett teknisk dokumentasjon.

---

# 32. Første milepæl

Første milepæl er:

> En bruker kan logge inn, registrere et kjøretøy og se kjøretøyets simulerte posisjon oppdateres i sanntid på et kart.

Ikke start med KI, relasjonsgraf eller avansert anomalideteksjon før denne flyten fungerer stabilt.

---

# 33. Andre milepæl

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

Tredje milepæl er:

> Brukeren kan åpne en incident, spille av hendelsen på kartet og se en kildebelagt forklaring av hvorfor hendelsen ble vurdert som mistenkelig.

---

# 35. Første oppgaver

Arbeid i denne rekkefølgen:

```text
1. Opprett repository og mappestruktur
2. Opprett ASP.NET Core-løsning
3. Opprett React-applikasjon
4. Sett opp Docker Compose
5. Sett opp PostgreSQL med PostGIS
6. Sett opp RabbitMQ og Redis
7. Lag health endpoints
8. Implementer Organization og Membership
9. Implementer authentication
10. Implementer Asset og Device
11. Lag telemetrikontrakt
12. Lag telemetry ingestion endpoint
13. Lag enkel simulator
14. Vis én bil på MapLibre-kartet
15. Oppdater bilens posisjon med SignalR
16. Utvid simulatoren til flere kjøretøy
17. Implementer første geofence-regel
18. Opprett første Detection
19. Opprett første Incident
20. Bygg hendelsestidslinjen
```

---

# 36. Første vertikale funksjon

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

* Validering
* Organisasjonsisolering
* Feilhåndtering
* Logging
* Integrationstest
* Realtime-oppdatering
* Loading state
* Offline-status

---

# 37. Tekniske beslutninger som skal dokumenteres

Opprett korte ADR-filer for:

```text
ADR-001: Modular monolith
ADR-002: PostgreSQL and PostGIS
ADR-003: RabbitMQ for telemetry processing
ADR-004: SignalR for realtime updates
ADR-005: Separate Python anomaly service
ADR-006: Multi-tenant data isolation
ADR-007: Rule-based detection before machine learning
ADR-008: Controlled AI tools with citations
ADR-009: OpenTelemetry observability
ADR-010: Simulated telemetry for public demo
```

Hver ADR skal inneholde:

* Problem
* Beslutning
* Alternativer
* Konsekvenser
* Begrunnelse

---

# 38. Definition of Done

En funksjon er ikke ferdig før:

* Backend validerer data
* Autorisasjon er kontrollert
* Organisasjonsisolering er kontrollert
* Feil håndteres
* Logger er strukturert
* Kritiske regler er testet
* API-kontrakten er dokumentert
* Frontend har loading state
* Frontend har error state
* Realtime-tilkoblingen håndterer reconnect
* CI passerer
* Funksjonen fungerer i Docker-miljøet

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

# 42. Nåværende prioritet

Den nåværende prioriteten er kun:

```text
Organization
Authentication
Asset
Device
Telemetry
Simulator
Live map
SignalR
```

Ikke implementer:

```text
KI
Anomalideteksjon
Relasjonsgraf
Rapportgenerator
Avanserte regler
```

før én simulert bil kan bevege seg stabilt på live-kartet gjennom hele systemarkitekturen.

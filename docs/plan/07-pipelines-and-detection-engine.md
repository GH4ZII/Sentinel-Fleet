# Pipelines og deteksjonsmotor

## Implementasjonsstatus

- [x] Telemetri-format og ingestion
- [x] Databehandlingsflyt (API → RabbitMQ → worker → lagring)
- [x] Regelmotor (geofence, arbeidstid, GPS offline, uautorisert bruker, drivstoff)
- [x] Anomalideteksjon (heuristikk → z-score → Isolation Forest)
- [x] Hendelseskorrelasjon

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

# Kjernefunksjoner

## Implementasjonsstatus

- [x] Organisasjoner, membership og roller (API)
- [x] Multi-tenant dataisolering
- [x] Asset-register og asset-detalj (API + UI)
- [x] Device API (opprettelse, nøkkelrotasjon)
- [ ] Devices-UI / administrasjonssider
- [ ] Org-/rolle-administrasjon i frontend
- [x] Telemetridatastrøm (simulerte data)

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

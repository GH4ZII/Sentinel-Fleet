# Deteksjoner, risiko og hendelsesmodell

## Implementasjonsstatus

### Deteksjonstyper (§7)

- [x] 7.1 Bruk utenfor arbeidstid
- [x] 7.2 Geofence-brudd
- [x] 7.3 Tap av GPS-signal
- [ ] 7.4 Kilometerstand går bakover
- [x] 7.5 Unaturlig drivstofftap
- [x] 7.6 Uautorisert bruker
- [ ] 7.7 Duplisert identitet
- [ ] 7.8 Bevegelse under verkstedstatus
- [x] 7.9 Uvanlig kjøremønster (via anomaly-service)
- [x] 7.10 Sammensatt tyveri/misbruk (via korrelasjon + risikoscore)

### Modell og rekonstruksjon (§8–10)

- [x] Risikoscore med forklaring
- [x] Signal → Detection → Incident
- [x] Hendelseskorrelasjon
- [x] Tidslinje / rekonstruksjon

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

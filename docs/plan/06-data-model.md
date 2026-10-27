# Datamodell

## Implementasjonsstatus

Kjerneentiteter er implementert i domenemodellen (EF Core + PostGIS). Noen planlagte felt/entiteter (f.eks. full WorkShift-UI, dedikert Audit-modul-API) er delvis eller kun brukt via Incidents.

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

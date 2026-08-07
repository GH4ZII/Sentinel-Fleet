namespace SentinelFleet.Domain.Incidents;

public enum IncidentType
{
    SuspiciousActivity = 0,
    PossibleTheft = 1,
    GeofenceViolation = 2,
    UnauthorizedUse = 3,
    FuelAnomaly = 4,
    GpsAnomaly = 5
}

namespace SentinelFleet.Domain.Rules;

public enum DetectionRuleType
{
    GeofenceEnter = 0,
    GeofenceExit = 1,
    OutsideWorkHours = 2,
    GpsOffline = 3,
    UnauthorizedUser = 4,
    FuelLoss = 5
}

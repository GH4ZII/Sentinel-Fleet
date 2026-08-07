using System.Text.Json;
using SentinelFleet.Domain.Rules;

namespace SentinelFleet.Infrastructure.Rules;

public static class RuleEvaluation
{
    public enum GeofenceTransition
    {
        None,
        Enter,
        Exit
    }

    public static GeofenceTransition GetTransition(bool? wasInside, bool isInside)
    {
        if (wasInside is null)
        {
            return GeofenceTransition.None;
        }

        if (wasInside == false && isInside)
        {
            return GeofenceTransition.Enter;
        }

        if (wasInside == true && !isInside)
        {
            return GeofenceTransition.Exit;
        }

        return GeofenceTransition.None;
    }

    public static bool IsOutsideWorkHours(
        DateTimeOffset recordedAtUtc,
        int workStartHourUtc,
        int workEndHourUtc)
    {
        var hour = recordedAtUtc.UtcDateTime.Hour;
        if (workStartHourUtc == workEndHourUtc)
        {
            return false;
        }

        if (workStartHourUtc < workEndHourUtc)
        {
            return hour < workStartHourUtc || hour >= workEndHourUtc;
        }

        // Overnight window e.g. 22-06 means work hours wrap midnight.
        return hour < workStartHourUtc && hour >= workEndHourUtc;
    }

    public static bool IsMoving(double? speedKph, bool? ignitionOn) =>
        (speedKph is > 1.0) || ignitionOn == true;

    public static bool IsFuelLoss(
        double? previousFuelPercent,
        double? currentFuelPercent,
        double? speedKph,
        bool? ignitionOn,
        double dropPercentThreshold,
        double maxSpeedKph)
    {
        if (previousFuelPercent is null || currentFuelPercent is null)
        {
            return false;
        }

        if (ignitionOn == true)
        {
            return false;
        }

        if (speedKph.HasValue && speedKph.Value > maxSpeedKph)
        {
            return false;
        }

        var drop = previousFuelPercent.Value - currentFuelPercent.Value;
        return drop >= dropPercentThreshold;
    }

    public static bool IsUnauthorizedDriver(Guid? driverUserId, bool hasActiveAssignment) =>
        driverUserId.HasValue && !hasActiveAssignment;

    public static RuleConfig ParseConfig(string? configurationJson)
    {
        if (string.IsNullOrWhiteSpace(configurationJson))
        {
            return RuleConfig.Defaults;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<RuleConfig>(
                configurationJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return parsed ?? RuleConfig.Defaults;
        }
        catch (JsonException)
        {
            return RuleConfig.Defaults;
        }
    }

    public static int RiskContribution(DetectionSeverity severity) =>
        severity switch
        {
            DetectionSeverity.Low => 10,
            DetectionSeverity.Medium => 20,
            DetectionSeverity.High => 35,
            DetectionSeverity.Critical => 50,
            _ => 15
        };
}

public sealed class RuleConfig
{
    public static RuleConfig Defaults { get; } = new();

    public int CooldownMinutes { get; init; } = 5;

    public int WorkStartHourUtc { get; init; } = 7;

    public int WorkEndHourUtc { get; init; } = 17;

    public int OfflineMinutes { get; init; } = 5;

    public double DropPercent { get; init; } = 8;

    public double MaxSpeedKph { get; init; } = 1;
}

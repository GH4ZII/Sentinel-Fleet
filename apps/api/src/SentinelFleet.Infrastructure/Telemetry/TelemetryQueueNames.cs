namespace SentinelFleet.Infrastructure.Telemetry;

public static class TelemetryQueueNames
{
    public const string Exchange = "sentinel.telemetry";
    public const string Queue = "telemetry.events";
    public const string RoutingKey = "telemetry.event";
}

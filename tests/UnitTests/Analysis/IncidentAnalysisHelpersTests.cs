using SentinelFleet.Application.Incidents;
using SentinelFleet.Domain.Detections;
using SentinelFleet.Domain.Incidents;
using SentinelFleet.Domain.Rules;

namespace SentinelFleet.UnitTests.Analysis;

public class IncidentAnalysisHelpersTests
{
    [Fact]
    public void MapIncidentType_UsageAnomaly_IsSuspiciousActivity()
    {
        var mapped = IncidentCorrelation.MapIncidentType(DetectionRuleType.UsageAnomaly);
        Assert.Equal(IncidentType.SuspiciousActivity, mapped);
    }

    [Fact]
    public void BuildTitle_IncludesHighestSeverityDetection()
    {
        var detections = new List<Detection>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid(),
                AssetId = Guid.NewGuid(),
                DetectionType = DetectionRuleType.UsageAnomaly,
                Severity = DetectionSeverity.Medium,
                Title = "Usage anomaly detected",
                TriggeredAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid(),
                AssetId = Guid.NewGuid(),
                DetectionType = DetectionRuleType.GeofenceExit,
                Severity = DetectionSeverity.Critical,
                Title = "Left geofence",
                TriggeredAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var title = IncidentCorrelation.BuildTitle(detections);
        Assert.Contains("Left geofence", title);
        Assert.Contains("+1", title);
    }
}

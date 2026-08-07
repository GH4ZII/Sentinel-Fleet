using SentinelFleet.Application.Incidents;
using SentinelFleet.Domain.Assets;
using SentinelFleet.Domain.Detections;
using SentinelFleet.Domain.Incidents;
using SentinelFleet.Domain.Rules;

namespace SentinelFleet.UnitTests.Incidents;

public class IncidentCorrelationTests
{
    [Fact]
    public void IsWithinWindow_AcceptsThirtyMinutes()
    {
        var baseTime = new DateTimeOffset(2026, 8, 7, 12, 0, 0, TimeSpan.Zero);
        Assert.True(IncidentCorrelation.IsWithinWindow(baseTime, baseTime.AddMinutes(30)));
        Assert.False(IncidentCorrelation.IsWithinWindow(baseTime, baseTime.AddMinutes(31)));
    }

    [Fact]
    public void IsOpenStatus_OnlyOpenAndInvestigating()
    {
        Assert.True(IncidentCorrelation.IsOpenStatus(IncidentStatus.Open));
        Assert.True(IncidentCorrelation.IsOpenStatus(IncidentStatus.Investigating));
        Assert.False(IncidentCorrelation.IsOpenStatus(IncidentStatus.Resolved));
        Assert.False(IncidentCorrelation.IsOpenStatus(IncidentStatus.Dismissed));
    }

    [Fact]
    public void InferType_EscalatesToPossibleTheft_WithThreeDistinctSignals()
    {
        var detections = new List<Detection>
        {
            Make(DetectionRuleType.GeofenceExit),
            Make(DetectionRuleType.GpsOffline),
            Make(DetectionRuleType.UnauthorizedUser)
        };

        Assert.Equal(IncidentType.PossibleTheft, IncidentCorrelation.InferTypeFromDetections(detections));
    }

    [Fact]
    public void BuildTitle_UsesSingleDetectionTitle()
    {
        var detections = new List<Detection> { Make(DetectionRuleType.FuelLoss, "Fuel loss detected") };
        Assert.Equal("Fuel loss detected", IncidentCorrelation.BuildTitle(detections));
    }

    private static Detection Make(DetectionRuleType type, string title = "Alert") =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            AssetId = Guid.NewGuid(),
            DetectionType = type,
            Severity = DetectionSeverity.High,
            Title = title,
            TriggeredAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };
}

public class RiskScoringTests
{
    [Fact]
    public void Calculate_SumsContributions_AndCapsAt100()
    {
        var detections = Enumerable.Range(0, 5)
            .Select(_ => new Detection
            {
                Id = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid(),
                AssetId = Guid.NewGuid(),
                DetectionType = DetectionRuleType.GeofenceExit,
                Severity = DetectionSeverity.Critical,
                RiskContribution = 50,
                Title = "Exit",
                TriggeredAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            })
            .ToList();

        // First 50 + half for repeats (25*4) = 150 → capped 100, plus compound not applicable alone
        var result = RiskScoring.Calculate(detections);
        Assert.Equal(100, result.Score);
        Assert.Equal(RiskLevel.Critical, result.RiskLevel);
    }

    [Fact]
    public void Calculate_AddsAssetCriticalityAndCompoundBonus()
    {
        var detections = new List<Detection>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid(),
                AssetId = Guid.NewGuid(),
                DetectionType = DetectionRuleType.GeofenceExit,
                Severity = DetectionSeverity.High,
                RiskContribution = 35,
                Title = "Geofence exit",
                TriggeredAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid(),
                AssetId = Guid.NewGuid(),
                DetectionType = DetectionRuleType.GpsOffline,
                Severity = DetectionSeverity.High,
                RiskContribution = 35,
                Title = "GPS offline",
                TriggeredAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var result = RiskScoring.Calculate(detections, AssetCriticality.Critical);
        // 35 + 35 + 15 criticality + 10 compound = 95
        Assert.Equal(95, result.Score);
        Assert.Equal(RiskLevel.Critical, result.RiskLevel);
        Assert.Contains(result.Factors, f => f.Code == "AssetCriticality");
        Assert.Contains(result.Factors, f => f.Code == "CompoundSignal");
    }

    [Theory]
    [InlineData(0, RiskLevel.Low)]
    [InlineData(29, RiskLevel.Low)]
    [InlineData(30, RiskLevel.Moderate)]
    [InlineData(59, RiskLevel.Moderate)]
    [InlineData(60, RiskLevel.High)]
    [InlineData(79, RiskLevel.High)]
    [InlineData(80, RiskLevel.Critical)]
    public void ToRiskLevel_UsesBands(int score, RiskLevel expected)
    {
        Assert.Equal(expected, RiskScoring.ToRiskLevel(score));
    }
}

using NetTopologySuite.Geometries;
using SentinelFleet.Application.Geofences;
using SentinelFleet.Infrastructure.Geofences;
using SentinelFleet.Infrastructure.Rules;

namespace SentinelFleet.UnitTests.Rules;

public class RuleEvaluationTests
{
    [Theory]
    [InlineData(null, true, RuleEvaluation.GeofenceTransition.None)]
    [InlineData(false, true, RuleEvaluation.GeofenceTransition.Enter)]
    [InlineData(true, false, RuleEvaluation.GeofenceTransition.Exit)]
    [InlineData(true, true, RuleEvaluation.GeofenceTransition.None)]
    [InlineData(false, false, RuleEvaluation.GeofenceTransition.None)]
    public void GetTransition_ReturnsExpected(
        bool? wasInside,
        bool isInside,
        RuleEvaluation.GeofenceTransition expected)
    {
        Assert.Equal(expected, RuleEvaluation.GetTransition(wasInside, isInside));
    }

    [Theory]
    [InlineData(6, 7, 17, true)]
    [InlineData(7, 7, 17, false)]
    [InlineData(12, 7, 17, false)]
    [InlineData(17, 7, 17, true)]
    [InlineData(22, 7, 17, true)]
    public void IsOutsideWorkHours_UsesUtcWindow(
        int hourUtc,
        int start,
        int end,
        bool expected)
    {
        var recordedAt = new DateTimeOffset(2026, 8, 7, hourUtc, 0, 0, TimeSpan.Zero);
        Assert.Equal(expected, RuleEvaluation.IsOutsideWorkHours(recordedAt, start, end));
    }

    [Fact]
    public void IsFuelLoss_DetectsDropWhileStationaryIgnitionOff()
    {
        Assert.True(RuleEvaluation.IsFuelLoss(
            previousFuelPercent: 60,
            currentFuelPercent: 45,
            speedKph: 0,
            ignitionOn: false,
            dropPercentThreshold: 8,
            maxSpeedKph: 1));
    }

    [Fact]
    public void IsFuelLoss_IgnoresWhenIgnitionOn()
    {
        Assert.False(RuleEvaluation.IsFuelLoss(
            previousFuelPercent: 60,
            currentFuelPercent: 45,
            speedKph: 0,
            ignitionOn: true,
            dropPercentThreshold: 8,
            maxSpeedKph: 1));
    }

    [Fact]
    public void IsFuelLoss_IgnoresSmallDrop()
    {
        Assert.False(RuleEvaluation.IsFuelLoss(
            previousFuelPercent: 60,
            currentFuelPercent: 55,
            speedKph: 0,
            ignitionOn: false,
            dropPercentThreshold: 8,
            maxSpeedKph: 1));
    }

    [Fact]
    public void IsUnauthorizedDriver_WhenDriverPresentWithoutAssignment()
    {
        Assert.True(RuleEvaluation.IsUnauthorizedDriver(Guid.NewGuid(), hasActiveAssignment: false));
        Assert.False(RuleEvaluation.IsUnauthorizedDriver(Guid.NewGuid(), hasActiveAssignment: true));
        Assert.False(RuleEvaluation.IsUnauthorizedDriver(null, hasActiveAssignment: false));
    }

    [Fact]
    public void IsMoving_RequiresSpeedOrIgnition()
    {
        Assert.True(RuleEvaluation.IsMoving(12, false));
        Assert.True(RuleEvaluation.IsMoving(0, true));
        Assert.False(RuleEvaluation.IsMoving(0.5, false));
        Assert.False(RuleEvaluation.IsMoving(null, null));
    }

    [Fact]
    public void ParseConfig_ReadsKnownFields()
    {
        var config = RuleEvaluation.ParseConfig(
            """{"cooldownMinutes":12,"workStartHourUtc":8,"dropPercent":10.5}""");

        Assert.Equal(12, config.CooldownMinutes);
        Assert.Equal(8, config.WorkStartHourUtc);
        Assert.Equal(10.5, config.DropPercent);
    }
}

public class GeofencePolygonTests
{
    [Fact]
    public void TryBuildPolygon_RequiresAtLeastThreePoints()
    {
        var tooFew = new List<CoordinateDto>
        {
            new(10.75, 59.91),
            new(10.76, 59.91)
        };

        Assert.Null(GeofenceService.TryBuildPolygon(tooFew));
    }

    [Fact]
    public void TryBuildPolygon_BuildsClosedPolygon()
    {
        var coords = new List<CoordinateDto>
        {
            new(10.74, 59.90),
            new(10.76, 59.90),
            new(10.76, 59.92),
            new(10.74, 59.92)
        };

        var polygon = GeofenceService.TryBuildPolygon(coords);
        Assert.NotNull(polygon);
        Assert.True(polygon!.IsValid);
        var factory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(4326);
        Assert.True(polygon.Contains(factory.CreatePoint(new Coordinate(10.75, 59.91))));
    }
}

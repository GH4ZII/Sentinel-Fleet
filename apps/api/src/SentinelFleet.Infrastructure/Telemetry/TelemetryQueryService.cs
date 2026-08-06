using Microsoft.EntityFrameworkCore;
using SentinelFleet.Application.Security;
using SentinelFleet.Application.Telemetry;
using SentinelFleet.Infrastructure.Persistence;

namespace SentinelFleet.Infrastructure.Telemetry;

public sealed class TelemetryQueryService(
    SentinelFleetDbContext db,
    IOrganizationContext organizationContext) : ITelemetryQueryService
{
    public async Task<TelemetryResult<LatestTelemetryDto>> GetLatestForAssetAsync(
        Guid assetId,
        CancellationToken cancellationToken = default)
    {
        var evt = await db.TelemetryEvents.AsNoTracking()
            .Where(e => e.OrganizationId == organizationContext.OrganizationId && e.AssetId == assetId)
            .OrderByDescending(e => e.RecordedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (evt is null)
        {
            return TelemetryResult<LatestTelemetryDto>.Failure(
                new TelemetryError(TelemetryErrorCode.NotFound, "No telemetry found for asset."));
        }

        return TelemetryResult<LatestTelemetryDto>.Success(ToLatest(evt));
    }

    public async Task<TelemetryResult<IReadOnlyList<AssetPositionDto>>> ListPositionsAsync(
        Guid assetId,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 500);

        var exists = await db.Assets.AsNoTracking()
            .AnyAsync(
                a => a.Id == assetId && a.OrganizationId == organizationContext.OrganizationId,
                cancellationToken);

        if (!exists)
        {
            return TelemetryResult<IReadOnlyList<AssetPositionDto>>.Failure(
                new TelemetryError(TelemetryErrorCode.NotFound, "Asset not found."));
        }

        var positions = await db.TelemetryEvents.AsNoTracking()
            .Where(e => e.OrganizationId == organizationContext.OrganizationId && e.AssetId == assetId)
            .OrderByDescending(e => e.RecordedAt)
            .Take(limit)
            .Select(e => new AssetPositionDto(
                e.AssetId,
                e.Latitude,
                e.Longitude,
                e.SpeedKph,
                e.Heading,
                e.RecordedAt))
            .ToListAsync(cancellationToken);

        return TelemetryResult<IReadOnlyList<AssetPositionDto>>.Success(positions);
    }

    public async Task<TelemetryResult<IReadOnlyList<LatestTelemetryDto>>> ListTelemetryAsync(
        Guid assetId,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 500);

        var exists = await db.Assets.AsNoTracking()
            .AnyAsync(
                a => a.Id == assetId && a.OrganizationId == organizationContext.OrganizationId,
                cancellationToken);

        if (!exists)
        {
            return TelemetryResult<IReadOnlyList<LatestTelemetryDto>>.Failure(
                new TelemetryError(TelemetryErrorCode.NotFound, "Asset not found."));
        }

        var events = await db.TelemetryEvents.AsNoTracking()
            .Where(e => e.OrganizationId == organizationContext.OrganizationId && e.AssetId == assetId)
            .OrderByDescending(e => e.RecordedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return TelemetryResult<IReadOnlyList<LatestTelemetryDto>>.Success(
            events.Select(ToLatest).ToList());
    }

    private static LatestTelemetryDto ToLatest(Domain.Telemetry.TelemetryEvent e) =>
        new(
            e.AssetId,
            e.DeviceId,
            e.EventId,
            e.EventType,
            e.RecordedAt,
            e.ReceivedAt,
            e.Latitude,
            e.Longitude,
            e.SpeedKph,
            e.Heading,
            e.IgnitionOn);
}

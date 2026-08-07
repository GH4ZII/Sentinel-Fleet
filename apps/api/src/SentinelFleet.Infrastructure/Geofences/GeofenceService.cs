using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using SentinelFleet.Application.Geofences;
using SentinelFleet.Application.Security;
using SentinelFleet.Domain.Geofences;
using SentinelFleet.Infrastructure.Persistence;

namespace SentinelFleet.Infrastructure.Geofences;

public sealed class GeofenceService(
    SentinelFleetDbContext db,
    IOrganizationContext organizationContext) : IGeofenceService
{
    private static readonly GeometryFactory GeometryFactory =
        NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public async Task<GeofenceResult<IReadOnlyList<GeofenceDto>>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await db.Geofences.AsNoTracking()
            .Where(g => g.OrganizationId == organizationContext.OrganizationId)
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);

        return GeofenceResult<IReadOnlyList<GeofenceDto>>.Success(items.Select(ToDto).ToList());
    }

    public async Task<GeofenceResult<GeofenceDto>> GetAsync(
        Guid geofenceId,
        CancellationToken cancellationToken = default)
    {
        var entity = await FindInOrgAsync(geofenceId, cancellationToken);
        if (entity is null)
        {
            return GeofenceResult<GeofenceDto>.Failure(
                new GeofenceError(GeofenceErrorCode.NotFound, "Geofence not found."));
        }

        return GeofenceResult<GeofenceDto>.Success(ToDto(entity));
    }

    public async Task<GeofenceResult<GeofenceDto>> CreateAsync(
        CreateGeofenceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!organizationContext.CanMutate)
        {
            return GeofenceResult<GeofenceDto>.Failure(
                new GeofenceError(GeofenceErrorCode.Forbidden, "Insufficient permissions."));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return GeofenceResult<GeofenceDto>.Failure(
                new GeofenceError(GeofenceErrorCode.Validation, "Name is required."));
        }

        var polygon = TryBuildPolygon(request.Coordinates);
        if (polygon is null)
        {
            return GeofenceResult<GeofenceDto>.Failure(
                new GeofenceError(
                    GeofenceErrorCode.Validation,
                    "At least 3 distinct coordinates are required to form a polygon."));
        }

        var now = DateTimeOffset.UtcNow;
        var entity = new Geofence
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationContext.OrganizationId,
            Name = request.Name.Trim(),
            Description = request.Description,
            Geometry = polygon,
            GeofenceType = request.GeofenceType,
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Geofences.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return GeofenceResult<GeofenceDto>.Success(ToDto(entity));
    }

    public async Task<GeofenceResult<GeofenceDto>> UpdateAsync(
        Guid geofenceId,
        UpdateGeofenceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!organizationContext.CanMutate)
        {
            return GeofenceResult<GeofenceDto>.Failure(
                new GeofenceError(GeofenceErrorCode.Forbidden, "Insufficient permissions."));
        }

        var entity = await FindInOrgAsync(geofenceId, cancellationToken);
        if (entity is null)
        {
            return GeofenceResult<GeofenceDto>.Failure(
                new GeofenceError(GeofenceErrorCode.NotFound, "Geofence not found."));
        }

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return GeofenceResult<GeofenceDto>.Failure(
                    new GeofenceError(GeofenceErrorCode.Validation, "Name is required."));
            }

            entity.Name = request.Name.Trim();
        }

        if (request.Description is not null)
        {
            entity.Description = request.Description;
        }

        if (request.GeofenceType is GeofenceType type)
        {
            entity.GeofenceType = type;
        }

        if (request.IsActive is bool active)
        {
            entity.IsActive = active;
        }

        if (request.Coordinates is not null)
        {
            var polygon = TryBuildPolygon(request.Coordinates);
            if (polygon is null)
            {
                return GeofenceResult<GeofenceDto>.Failure(
                    new GeofenceError(
                        GeofenceErrorCode.Validation,
                        "At least 3 distinct coordinates are required to form a polygon."));
            }

            entity.Geometry = polygon;
        }

        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return GeofenceResult<GeofenceDto>.Success(ToDto(entity));
    }

    public async Task<GeofenceResult> DeleteAsync(
        Guid geofenceId,
        CancellationToken cancellationToken = default)
    {
        if (!organizationContext.CanMutate)
        {
            return GeofenceResult.Failure(
                new GeofenceError(GeofenceErrorCode.Forbidden, "Insufficient permissions."));
        }

        var entity = await FindInOrgAsync(geofenceId, cancellationToken);
        if (entity is null)
        {
            return GeofenceResult.Failure(
                new GeofenceError(GeofenceErrorCode.NotFound, "Geofence not found."));
        }

        db.Geofences.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return GeofenceResult.Success();
    }

    public async Task<GeofenceResult<IReadOnlyList<AssetGeofenceDto>>> ListLinkedAssetsAsync(
        Guid geofenceId,
        CancellationToken cancellationToken = default)
    {
        var exists = await db.Geofences.AnyAsync(
            g => g.Id == geofenceId && g.OrganizationId == organizationContext.OrganizationId,
            cancellationToken);
        if (!exists)
        {
            return GeofenceResult<IReadOnlyList<AssetGeofenceDto>>.Failure(
                new GeofenceError(GeofenceErrorCode.NotFound, "Geofence not found."));
        }

        var links = await db.AssetGeofences.AsNoTracking()
            .Where(a => a.GeofenceId == geofenceId &&
                        a.OrganizationId == organizationContext.OrganizationId)
            .OrderBy(a => a.AssetId)
            .Select(a => new AssetGeofenceDto(
                a.Id,
                a.AssetId,
                a.GeofenceId,
                a.RuleType,
                a.ValidFrom,
                a.ValidTo))
            .ToListAsync(cancellationToken);

        return GeofenceResult<IReadOnlyList<AssetGeofenceDto>>.Success(links);
    }

    public async Task<GeofenceResult<AssetGeofenceDto>> LinkAssetAsync(
        Guid geofenceId,
        LinkAssetGeofenceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!organizationContext.CanMutate)
        {
            return GeofenceResult<AssetGeofenceDto>.Failure(
                new GeofenceError(GeofenceErrorCode.Forbidden, "Insufficient permissions."));
        }

        var geofence = await FindInOrgAsync(geofenceId, cancellationToken);
        if (geofence is null)
        {
            return GeofenceResult<AssetGeofenceDto>.Failure(
                new GeofenceError(GeofenceErrorCode.NotFound, "Geofence not found."));
        }

        var assetExists = await db.Assets.AnyAsync(
            a => a.Id == request.AssetId && a.OrganizationId == organizationContext.OrganizationId,
            cancellationToken);
        if (!assetExists)
        {
            return GeofenceResult<AssetGeofenceDto>.Failure(
                new GeofenceError(GeofenceErrorCode.NotFound, "Asset not found."));
        }

        var existing = await db.AssetGeofences.FirstOrDefaultAsync(
            a => a.GeofenceId == geofenceId && a.AssetId == request.AssetId,
            cancellationToken);
        if (existing is not null)
        {
            existing.RuleType = request.RuleType;
            existing.ValidFrom = request.ValidFrom;
            existing.ValidTo = request.ValidTo;
            await db.SaveChangesAsync(cancellationToken);
            return GeofenceResult<AssetGeofenceDto>.Success(ToLinkDto(existing));
        }

        var link = new AssetGeofence
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationContext.OrganizationId,
            AssetId = request.AssetId,
            GeofenceId = geofenceId,
            RuleType = request.RuleType,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo
        };

        db.AssetGeofences.Add(link);
        await db.SaveChangesAsync(cancellationToken);
        return GeofenceResult<AssetGeofenceDto>.Success(ToLinkDto(link));
    }

    public async Task<GeofenceResult> UnlinkAssetAsync(
        Guid geofenceId,
        Guid assetId,
        CancellationToken cancellationToken = default)
    {
        if (!organizationContext.CanMutate)
        {
            return GeofenceResult.Failure(
                new GeofenceError(GeofenceErrorCode.Forbidden, "Insufficient permissions."));
        }

        var link = await db.AssetGeofences.FirstOrDefaultAsync(
            a => a.GeofenceId == geofenceId &&
                 a.AssetId == assetId &&
                 a.OrganizationId == organizationContext.OrganizationId,
            cancellationToken);
        if (link is null)
        {
            return GeofenceResult.Failure(
                new GeofenceError(GeofenceErrorCode.NotFound, "Asset geofence link not found."));
        }

        db.AssetGeofences.Remove(link);
        await db.SaveChangesAsync(cancellationToken);
        return GeofenceResult.Success();
    }

    private async Task<Geofence?> FindInOrgAsync(Guid geofenceId, CancellationToken cancellationToken)
    {
        return await db.Geofences.FirstOrDefaultAsync(
            g => g.Id == geofenceId && g.OrganizationId == organizationContext.OrganizationId,
            cancellationToken);
    }

    public static Polygon? TryBuildPolygon(IReadOnlyList<CoordinateDto> coordinates)
    {
        if (coordinates.Count < 3)
        {
            return null;
        }

        var points = coordinates
            .Select(c => new Coordinate(c.Longitude, c.Latitude))
            .ToList();

        if (!points[0].Equals2D(points[^1]))
        {
            points.Add(new Coordinate(points[0]));
        }

        if (points.Count < 4)
        {
            return null;
        }

        var ring = GeometryFactory.CreateLinearRing(points.ToArray());
        var polygon = GeometryFactory.CreatePolygon(ring);
        if (!polygon.IsValid || polygon.Area <= 0)
        {
            return null;
        }

        return polygon;
    }

    private static GeofenceDto ToDto(Geofence entity)
    {
        var ring = entity.Geometry.ExteriorRing.Coordinates
            .Select(c => new[] { c.X, c.Y })
            .ToList();

        return new GeofenceDto(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.GeofenceType,
            entity.IsActive,
            new PolygonGeometryDto("Polygon", [ring]),
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    private static AssetGeofenceDto ToLinkDto(AssetGeofence link) =>
        new(link.Id, link.AssetId, link.GeofenceId, link.RuleType, link.ValidFrom, link.ValidTo);
}

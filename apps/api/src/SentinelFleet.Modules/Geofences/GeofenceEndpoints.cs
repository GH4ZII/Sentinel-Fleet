using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SentinelFleet.Application.Geofences;

namespace SentinelFleet.Modules.Geofences;

public static class GeofenceEndpoints
{
    public static IEndpointRouteBuilder MapGeofenceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/geofences")
            .WithTags("Geofences")
            .RequireAuthorization();

        group.MapGet("/", ListAsync);
        group.MapPost("/", CreateAsync);
        group.MapGet("/{geofenceId:guid}", GetAsync);
        group.MapPatch("/{geofenceId:guid}", UpdateAsync);
        group.MapDelete("/{geofenceId:guid}", DeleteAsync);
        group.MapGet("/{geofenceId:guid}/assets", ListLinksAsync);
        group.MapPost("/{geofenceId:guid}/assets", LinkAssetAsync);
        group.MapDelete("/{geofenceId:guid}/assets/{assetId:guid}", UnlinkAssetAsync);

        return endpoints;
    }

    private static async Task<IResult> ListAsync(IGeofenceService service, CancellationToken ct)
    {
        var result = await service.ListAsync(ct);
        return ToHttp(result);
    }

    private static async Task<IResult> CreateAsync(
        CreateGeofenceRequest request,
        IGeofenceService service,
        CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return ToHttp(result, StatusCodes.Status201Created);
    }

    private static async Task<IResult> GetAsync(
        Guid geofenceId,
        IGeofenceService service,
        CancellationToken ct)
    {
        var result = await service.GetAsync(geofenceId, ct);
        return ToHttp(result);
    }

    private static async Task<IResult> UpdateAsync(
        Guid geofenceId,
        UpdateGeofenceRequest request,
        IGeofenceService service,
        CancellationToken ct)
    {
        var result = await service.UpdateAsync(geofenceId, request, ct);
        return ToHttp(result);
    }

    private static async Task<IResult> DeleteAsync(
        Guid geofenceId,
        IGeofenceService service,
        CancellationToken ct)
    {
        var result = await service.DeleteAsync(geofenceId, ct);
        return ToHttp(result);
    }

    private static async Task<IResult> ListLinksAsync(
        Guid geofenceId,
        IGeofenceService service,
        CancellationToken ct)
    {
        var result = await service.ListLinkedAssetsAsync(geofenceId, ct);
        return ToHttp(result);
    }

    private static async Task<IResult> LinkAssetAsync(
        Guid geofenceId,
        LinkAssetGeofenceRequest request,
        IGeofenceService service,
        CancellationToken ct)
    {
        var result = await service.LinkAssetAsync(geofenceId, request, ct);
        return ToHttp(result, StatusCodes.Status201Created);
    }

    private static async Task<IResult> UnlinkAssetAsync(
        Guid geofenceId,
        Guid assetId,
        IGeofenceService service,
        CancellationToken ct)
    {
        var result = await service.UnlinkAssetAsync(geofenceId, assetId, ct);
        return ToHttp(result);
    }

    private static IResult ToHttp(GeofenceResult result)
    {
        if (!result.Succeeded)
        {
            return MapError(result.Error!);
        }

        return Results.NoContent();
    }

    private static IResult ToHttp<T>(GeofenceResult<T> result, int successStatus = StatusCodes.Status200OK)
    {
        if (!result.Succeeded)
        {
            return MapError(result.Error!);
        }

        return successStatus == StatusCodes.Status201Created
            ? Results.Created($"/api/v1/geofences", result.Value)
            : Results.Ok(result.Value);
    }

    private static IResult MapError(GeofenceError error) =>
        error.Code switch
        {
            GeofenceErrorCode.Validation => Results.BadRequest(new { error = error.Message }),
            GeofenceErrorCode.NotFound => Results.NotFound(new { error = error.Message }),
            GeofenceErrorCode.Forbidden => Results.Json(
                new { error = error.Message },
                statusCode: StatusCodes.Status403Forbidden),
            _ => Results.Problem(error.Message)
        };
}

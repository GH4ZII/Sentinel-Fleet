using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SentinelFleet.Application.Assets;

namespace SentinelFleet.Modules.Assets;

public static class AssetEndpoints
{
    public static IEndpointRouteBuilder MapAssetEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var types = endpoints.MapGroup("/api/v1/asset-types")
            .WithTags("AssetTypes")
            .RequireAuthorization();

        types.MapGet("/", ListTypesAsync);
        types.MapPost("/", CreateTypeAsync);

        var assets = endpoints.MapGroup("/api/v1/assets")
            .WithTags("Assets")
            .RequireAuthorization();

        assets.MapGet("/", ListAssetsAsync);
        assets.MapPost("/", CreateAssetAsync);
        assets.MapGet("/{assetId:guid}", GetAssetAsync);
        assets.MapPatch("/{assetId:guid}", UpdateAssetAsync);
        assets.MapGet("/{assetId:guid}/current-status", GetStatusAsync);
        assets.MapGet("/{assetId:guid}/telemetry", EmptyListAsync);
        assets.MapGet("/{assetId:guid}/positions", EmptyListAsync);
        assets.MapGet("/{assetId:guid}/incidents", EmptyListAsync);

        return endpoints;
    }

    private static async Task<IResult> ListTypesAsync(IAssetService service, CancellationToken ct)
    {
        var result = await service.ListAssetTypesAsync(ct);
        return ToHttp(result);
    }

    private static async Task<IResult> CreateTypeAsync(
        CreateAssetTypeRequest request,
        IAssetService service,
        CancellationToken ct)
    {
        var result = await service.CreateAssetTypeAsync(request, ct);
        return ToHttp(result, StatusCodes.Status201Created);
    }

    private static async Task<IResult> ListAssetsAsync(IAssetService service, CancellationToken ct)
    {
        var result = await service.ListAssetsAsync(ct);
        return ToHttp(result);
    }

    private static async Task<IResult> CreateAssetAsync(
        CreateAssetRequest request,
        IAssetService service,
        CancellationToken ct)
    {
        var result = await service.CreateAssetAsync(request, ct);
        return ToHttp(result, StatusCodes.Status201Created);
    }

    private static async Task<IResult> GetAssetAsync(
        Guid assetId,
        IAssetService service,
        CancellationToken ct)
    {
        var result = await service.GetAssetAsync(assetId, ct);
        return ToHttp(result);
    }

    private static async Task<IResult> UpdateAssetAsync(
        Guid assetId,
        UpdateAssetRequest request,
        IAssetService service,
        CancellationToken ct)
    {
        var result = await service.UpdateAssetAsync(assetId, request, ct);
        return ToHttp(result);
    }

    private static async Task<IResult> GetStatusAsync(
        Guid assetId,
        IAssetService service,
        CancellationToken ct)
    {
        var result = await service.GetCurrentStatusAsync(assetId, ct);
        return ToHttp(result);
    }

    private static IResult EmptyListAsync() => Results.Ok(Array.Empty<object>());

    private static IResult ToHttp<T>(AssetResult<T> result, int successStatus = StatusCodes.Status200OK)
    {
        if (!result.Succeeded)
        {
            return result.Error!.Code switch
            {
                AssetErrorCode.Validation => Results.BadRequest(new { error = result.Error.Message }),
                AssetErrorCode.NotFound => Results.NotFound(new { error = result.Error.Message }),
                AssetErrorCode.Forbidden => Results.Json(
                    new { error = result.Error.Message },
                    statusCode: StatusCodes.Status403Forbidden),
                _ => Results.Problem(result.Error.Message)
            };
        }

        return successStatus == StatusCodes.Status201Created
            ? Results.Created($"/api/v1/assets", result.Value)
            : Results.Ok(result.Value);
    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SentinelFleet.Application.Detections;
using SentinelFleet.Domain.Rules;

namespace SentinelFleet.Modules.Detections;

public static class DetectionEndpoints
{
    public static IEndpointRouteBuilder MapDetectionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/detections")
            .WithTags("Detections")
            .RequireAuthorization();

        group.MapGet("/", ListAsync);
        group.MapGet("/{detectionId:guid}", GetAsync);

        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        IDetectionQueryService service,
        Guid? assetId,
        DetectionRuleType? detectionType,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int? limit,
        CancellationToken ct)
    {
        var result = await service.ListAsync(
            assetId,
            detectionType,
            from,
            to,
            limit ?? 100,
            ct);
        return ToHttp(result);
    }

    private static async Task<IResult> GetAsync(
        Guid detectionId,
        IDetectionQueryService service,
        CancellationToken ct)
    {
        var result = await service.GetAsync(detectionId, ct);
        return ToHttp(result);
    }

    private static IResult ToHttp<T>(DetectionResult<T> result)
    {
        if (!result.Succeeded)
        {
            return result.Error!.Code switch
            {
                DetectionErrorCode.Validation => Results.BadRequest(new { error = result.Error.Message }),
                DetectionErrorCode.NotFound => Results.NotFound(new { error = result.Error.Message }),
                DetectionErrorCode.Forbidden => Results.Json(
                    new { error = result.Error.Message },
                    statusCode: StatusCodes.Status403Forbidden),
                _ => Results.Problem(result.Error.Message)
            };
        }

        return Results.Ok(result.Value);
    }
}

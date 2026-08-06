using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SentinelFleet.Application.Telemetry;

namespace SentinelFleet.Modules.Telemetry;

public static class TelemetryEndpoints
{
    public const string ApiKeyHeaderName = "X-Api-Key";

    public static IEndpointRouteBuilder MapTelemetryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var ingest = endpoints.MapGroup("/api/v1/telemetry")
            .WithTags("Telemetry");

        ingest.MapPost("/events", IngestEventAsync);
        ingest.MapPost("/batch", IngestBatchAsync);

        var query = endpoints.MapGroup("/api/v1/telemetry")
            .WithTags("Telemetry")
            .RequireAuthorization();

        query.MapGet("/assets/{assetId:guid}/latest", GetLatestAsync);

        return endpoints;
    }

    private static async Task<IResult> IngestEventAsync(
        HttpRequest httpRequest,
        IngestTelemetryEventRequest request,
        ITelemetryIngestService service,
        CancellationToken cancellationToken)
    {
        if (!httpRequest.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeader) ||
            string.IsNullOrWhiteSpace(apiKeyHeader))
        {
            return Results.Unauthorized();
        }

        var result = await service.IngestAsync(apiKeyHeader.ToString(), request, cancellationToken);
        return ToHttp(result, StatusCodes.Status202Accepted);
    }

    private static async Task<IResult> IngestBatchAsync(
        HttpRequest httpRequest,
        IngestTelemetryBatchRequest request,
        ITelemetryIngestService service,
        CancellationToken cancellationToken)
    {
        if (!httpRequest.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeader) ||
            string.IsNullOrWhiteSpace(apiKeyHeader))
        {
            return Results.Unauthorized();
        }

        var result = await service.IngestBatchAsync(apiKeyHeader.ToString(), request, cancellationToken);
        return ToHttp(result, StatusCodes.Status202Accepted);
    }

    private static async Task<IResult> GetLatestAsync(
        Guid assetId,
        ITelemetryQueryService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetLatestForAssetAsync(assetId, cancellationToken);
        return ToHttp(result);
    }

    private static IResult ToHttp<T>(TelemetryResult<T> result, int successStatus = StatusCodes.Status200OK)
    {
        if (!result.Succeeded)
        {
            return result.Error!.Code switch
            {
                TelemetryErrorCode.Validation => Results.BadRequest(new { error = result.Error.Message }),
                TelemetryErrorCode.NotFound => Results.NotFound(new { error = result.Error.Message }),
                TelemetryErrorCode.Unauthorized => Results.Unauthorized(),
                TelemetryErrorCode.Forbidden => Results.Json(
                    new { error = result.Error.Message },
                    statusCode: StatusCodes.Status403Forbidden),
                _ => Results.Problem(result.Error.Message)
            };
        }

        return successStatus switch
        {
            StatusCodes.Status202Accepted => Results.Json(result.Value, statusCode: StatusCodes.Status202Accepted),
            _ => Results.Ok(result.Value)
        };
    }
}

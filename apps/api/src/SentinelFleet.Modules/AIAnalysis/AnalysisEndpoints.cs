using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SentinelFleet.Application.Analysis;

namespace SentinelFleet.Modules.AIAnalysis;

public static class AnalysisEndpoints
{
    public static IEndpointRouteBuilder MapAnalysisEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/incidents/{incidentId:guid}/analysis")
            .WithTags("Analysis")
            .RequireAuthorization();

        group.MapPost("/summary", SummarizeAsync);
        group.MapPost("/explain-risk", ExplainRiskAsync);
        group.MapPost("/missing-data", MissingDataAsync);
        group.MapPost("/similar-incidents", SimilarIncidentsAsync);
        group.MapPost("/report", GenerateReportAsync);

        var incidentGroup = endpoints.MapGroup("/api/v1/incidents/{incidentId:guid}")
            .WithTags("Analysis")
            .RequireAuthorization();

        incidentGroup.MapGet("/graph", GetGraphAsync);

        return endpoints;
    }

    private static async Task<IResult> SummarizeAsync(
        Guid incidentId,
        IIncidentAnalysisService service,
        CancellationToken ct)
    {
        var result = await service.SummarizeAsync(incidentId, ct);
        return ToHttp(result);
    }

    private static async Task<IResult> ExplainRiskAsync(
        Guid incidentId,
        IIncidentAnalysisService service,
        CancellationToken ct)
    {
        var result = await service.ExplainRiskAsync(incidentId, ct);
        return ToHttp(result);
    }

    private static async Task<IResult> MissingDataAsync(
        Guid incidentId,
        IIncidentAnalysisService service,
        CancellationToken ct)
    {
        var result = await service.MissingDataAsync(incidentId, ct);
        return ToHttp(result);
    }

    private static async Task<IResult> SimilarIncidentsAsync(
        Guid incidentId,
        IIncidentAnalysisService service,
        CancellationToken ct)
    {
        var result = await service.SimilarIncidentsAsync(incidentId, ct);
        return ToHttp(result);
    }

    private static async Task<IResult> GenerateReportAsync(
        Guid incidentId,
        IIncidentAnalysisService service,
        CancellationToken ct)
    {
        var result = await service.GenerateReportAsync(incidentId, ct);
        return ToHttp(result);
    }

    private static async Task<IResult> GetGraphAsync(
        Guid incidentId,
        IIncidentAnalysisService service,
        int? maxLevels,
        string? relationshipType,
        CancellationToken ct)
    {
        var result = await service.GetGraphAsync(incidentId, maxLevels ?? 2, relationshipType, ct);
        return ToHttp(result);
    }

    private static IResult ToHttp<T>(AnalysisResult<T> result)
    {
        if (!result.Succeeded)
        {
            return result.Error!.Code switch
            {
                AnalysisErrorCode.Validation => Results.BadRequest(new { error = result.Error.Message }),
                AnalysisErrorCode.NotFound => Results.NotFound(new { error = result.Error.Message }),
                AnalysisErrorCode.Forbidden => Results.Json(
                    new { error = result.Error.Message },
                    statusCode: StatusCodes.Status403Forbidden),
                _ => Results.Problem(result.Error.Message)
            };
        }

        return Results.Ok(result.Value);
    }
}

namespace SentinelFleet.Application.Analysis;

public sealed record CitationDto(
    string Claim,
    string SourceType,
    string SourceId,
    string? Detail);

public sealed record AnalysisStatementDto(
    string Kind,
    string Text,
    IReadOnlyList<CitationDto> Citations);

public sealed record IncidentSummaryDto(
    string Summary,
    IReadOnlyList<AnalysisStatementDto> Facts,
    IReadOnlyList<AnalysisStatementDto> Suspicions,
    IReadOnlyList<AnalysisStatementDto> Assumptions,
    IReadOnlyList<string> MissingData,
    IReadOnlyList<CitationDto> Citations,
    string AnalystVersion);

public sealed record RiskExplanationDto(
    int RiskScore,
    string RiskLevel,
    string Explanation,
    IReadOnlyList<AnalysisStatementDto> Factors,
    IReadOnlyList<CitationDto> Citations,
    string AnalystVersion);

public sealed record MissingDataDto(
    IReadOnlyList<string> MissingData,
    IReadOnlyList<string> SuggestedActions,
    IReadOnlyList<CitationDto> Citations,
    string AnalystVersion);

public sealed record SimilarIncidentDto(
    Guid IncidentId,
    string Title,
    string IncidentType,
    int RiskScore,
    string Status,
    DateTimeOffset DetectedAt,
    double Similarity,
    string Reason);

public sealed record SimilarIncidentsDto(
    IReadOnlyList<SimilarIncidentDto> Incidents,
    IReadOnlyList<CitationDto> Citations,
    string AnalystVersion);

public sealed record IncidentReportDto(
    string Title,
    string Narrative,
    IncidentSummaryDto Analysis,
    RiskExplanationDto Risk,
    MissingDataDto Gaps,
    IReadOnlyList<SimilarIncidentDto> SimilarIncidents,
    IReadOnlyList<CitationDto> AllCitations,
    DateTimeOffset GeneratedAt,
    string AnalystVersion);

public sealed record GraphNodeDto(
    string Id,
    string EntityType,
    Guid EntityId,
    string Label,
    string? Subtitle,
    int Level);

public sealed record GraphEdgeDto(
    string Id,
    string SourceId,
    string TargetId,
    string RelationshipType);

public sealed record IncidentGraphDto(
    Guid IncidentId,
    IReadOnlyList<GraphNodeDto> Nodes,
    IReadOnlyList<GraphEdgeDto> Edges,
    IReadOnlyList<string> RelationshipTypes);

public enum AnalysisErrorCode
{
    Validation,
    NotFound,
    Forbidden
}

public sealed record AnalysisError(AnalysisErrorCode Code, string Message);

public class AnalysisResult
{
    public bool Succeeded { get; init; }

    public AnalysisError? Error { get; init; }

    public static AnalysisResult Success() => new() { Succeeded = true };

    public static AnalysisResult Failure(AnalysisError error) =>
        new() { Succeeded = false, Error = error };
}

public sealed class AnalysisResult<T> : AnalysisResult
{
    public T? Value { get; init; }

    public static AnalysisResult<T> Success(T value) =>
        new() { Succeeded = true, Value = value };

    public static new AnalysisResult<T> Failure(AnalysisError error) =>
        new() { Succeeded = false, Error = error };
}

public interface IIncidentAnalysisService
{
    Task<AnalysisResult<IncidentSummaryDto>> SummarizeAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);

    Task<AnalysisResult<RiskExplanationDto>> ExplainRiskAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);

    Task<AnalysisResult<MissingDataDto>> MissingDataAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);

    Task<AnalysisResult<SimilarIncidentsDto>> SimilarIncidentsAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);

    Task<AnalysisResult<IncidentReportDto>> GenerateReportAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);

    Task<AnalysisResult<IncidentGraphDto>> GetGraphAsync(
        Guid incidentId,
        int maxLevels = 2,
        string? relationshipType = null,
        CancellationToken cancellationToken = default);
}

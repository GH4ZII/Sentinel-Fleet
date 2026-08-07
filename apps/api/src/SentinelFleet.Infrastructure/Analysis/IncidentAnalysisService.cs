using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SentinelFleet.Application.Analysis;
using SentinelFleet.Application.Security;
using SentinelFleet.Domain.Incidents;
using SentinelFleet.Domain.Rules;
using SentinelFleet.Infrastructure.Persistence;

namespace SentinelFleet.Infrastructure.Analysis;

/// <summary>
/// Controlled incident analyst: tool-backed, citation-first, no free-form DB access.
/// Deterministic for local demo; tool contracts match the project plan agent surface.
/// </summary>
public sealed class IncidentAnalysisService(
    SentinelFleetDbContext db,
    IOrganizationContext organizationContext,
    ILogger<IncidentAnalysisService> logger) : IIncidentAnalysisService
{
    public const string AnalystVersion = "v1-controlled-tools";

    public async Task<AnalysisResult<IncidentSummaryDto>> SummarizeAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        var ctx = await LoadContextAsync(incidentId, cancellationToken);
        if (ctx.Error is not null)
        {
            return AnalysisResult<IncidentSummaryDto>.Failure(ctx.Error);
        }

        LogTool("get_incident", incidentId);
        var summary = BuildSummary(ctx.Data!);
        return AnalysisResult<IncidentSummaryDto>.Success(summary);
    }

    public async Task<AnalysisResult<RiskExplanationDto>> ExplainRiskAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        var ctx = await LoadContextAsync(incidentId, cancellationToken);
        if (ctx.Error is not null)
        {
            return AnalysisResult<RiskExplanationDto>.Failure(ctx.Error);
        }

        LogTool("calculate_incident_risk", incidentId);
        return AnalysisResult<RiskExplanationDto>.Success(BuildRiskExplanation(ctx.Data!));
    }

    public async Task<AnalysisResult<MissingDataDto>> MissingDataAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        var ctx = await LoadContextAsync(incidentId, cancellationToken);
        if (ctx.Error is not null)
        {
            return AnalysisResult<MissingDataDto>.Failure(ctx.Error);
        }

        LogTool("get_incident_timeline", incidentId);
        return AnalysisResult<MissingDataDto>.Success(BuildMissingData(ctx.Data!));
    }

    public async Task<AnalysisResult<SimilarIncidentsDto>> SimilarIncidentsAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        var ctx = await LoadContextAsync(incidentId, cancellationToken);
        if (ctx.Error is not null)
        {
            return AnalysisResult<SimilarIncidentsDto>.Failure(ctx.Error);
        }

        LogTool("search_similar_incidents", incidentId);
        var similar = await SearchSimilarAsync(ctx.Data!, cancellationToken);
        return AnalysisResult<SimilarIncidentsDto>.Success(similar);
    }

    public async Task<AnalysisResult<IncidentReportDto>> GenerateReportAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        var ctx = await LoadContextAsync(incidentId, cancellationToken);
        if (ctx.Error is not null)
        {
            return AnalysisResult<IncidentReportDto>.Failure(ctx.Error);
        }

        LogTool("generate_incident_report", incidentId);
        var data = ctx.Data!;
        var analysis = BuildSummary(data);
        var risk = BuildRiskExplanation(data);
        var gaps = BuildMissingData(data);
        var similar = await SearchSimilarAsync(data, cancellationToken);

        var narrative = BuildNarrative(data, analysis, risk);
        var allCitations = analysis.Citations
            .Concat(risk.Citations)
            .Concat(gaps.Citations)
            .Concat(similar.Citations)
            .GroupBy(c => $"{c.SourceType}:{c.SourceId}:{c.Claim}")
            .Select(g => g.First())
            .ToList();

        var report = new IncidentReportDto(
            Title: $"Investigation report: {data.Incident.Title}",
            Narrative: narrative,
            Analysis: analysis,
            Risk: risk,
            Gaps: gaps,
            SimilarIncidents: similar.Incidents,
            AllCitations: allCitations,
            GeneratedAt: DateTimeOffset.UtcNow,
            AnalystVersion: AnalystVersion);

        db.AuditLogs.Add(new Domain.Audit.AuditLog
        {
            Id = Guid.NewGuid(),
            OrganizationId = data.Incident.OrganizationId,
            UserId = organizationContext.UserId,
            Action = "IncidentReportGenerated",
            EntityType = "Incident",
            EntityId = incidentId,
            NewValues = JsonSerializer.Serialize(new
            {
                analystVersion = AnalystVersion,
                citationCount = allCitations.Count,
                riskScore = data.Incident.RiskScore
            }),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);

        return AnalysisResult<IncidentReportDto>.Success(report);
    }

    public async Task<AnalysisResult<IncidentGraphDto>> GetGraphAsync(
        Guid incidentId,
        int maxLevels = 2,
        string? relationshipType = null,
        CancellationToken cancellationToken = default)
    {
        if (maxLevels is < 1 or > 3)
        {
            return AnalysisResult<IncidentGraphDto>.Failure(
                new AnalysisError(AnalysisErrorCode.Validation, "maxLevels must be between 1 and 3."));
        }

        var incident = await db.Incidents.AsNoTracking()
            .FirstOrDefaultAsync(
                i => i.Id == incidentId && i.OrganizationId == organizationContext.OrganizationId,
                cancellationToken);

        if (incident is null)
        {
            return AnalysisResult<IncidentGraphDto>.Failure(
                new AnalysisError(AnalysisErrorCode.NotFound, "Incident not found."));
        }

        LogTool("get_related_people", incidentId);

        var relationships = await db.IncidentEntities.AsNoTracking()
            .Where(e => e.IncidentId == incidentId)
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(relationshipType))
        {
            relationships = relationships
                .Where(r => string.Equals(r.RelationshipType, relationshipType, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var asset = await db.Assets.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == incident.PrimaryAssetId, cancellationToken);

        var nodes = new Dictionary<string, GraphNodeDto>(StringComparer.Ordinal);
        var edges = new List<GraphEdgeDto>();

        var incidentNodeId = $"Incident:{incident.Id}";
        nodes[incidentNodeId] = new GraphNodeDto(
            incidentNodeId,
            "Incident",
            incident.Id,
            incident.Title,
            $"{incident.Status} · risk {incident.RiskScore}",
            0);

        var assetNodeId = $"Asset:{incident.PrimaryAssetId}";
        nodes[assetNodeId] = new GraphNodeDto(
            assetNodeId,
            "Asset",
            incident.PrimaryAssetId,
            asset?.Name ?? incident.PrimaryAssetId.ToString()[..8],
            asset?.RegistrationNumber,
            1);
        edges.Add(new GraphEdgeDto(
            $"{incidentNodeId}->involved->{assetNodeId}",
            incidentNodeId,
            assetNodeId,
            "involved"));

        foreach (var rel in relationships)
        {
            if (maxLevels < 1)
            {
                continue;
            }

            var nodeId = $"{rel.EntityType}:{rel.EntityId}";
            if (!nodes.ContainsKey(nodeId))
            {
                var label = await ResolveLabelAsync(rel.EntityType, rel.EntityId, cancellationToken);
                nodes[nodeId] = new GraphNodeDto(
                    nodeId,
                    rel.EntityType,
                    rel.EntityId,
                    label,
                    rel.RelationshipType,
                    1);
            }

            edges.Add(new GraphEdgeDto(
                $"{incidentNodeId}->{rel.RelationshipType}->{nodeId}",
                incidentNodeId,
                nodeId,
                rel.RelationshipType));
        }

        // Level 2: geofences linked via detections metadata / asset geofences for primary asset.
        if (maxLevels >= 2)
        {
            var geofenceIds = await db.AssetGeofences.AsNoTracking()
                .Where(ag => ag.AssetId == incident.PrimaryAssetId)
                .Select(ag => ag.GeofenceId)
                .Take(8)
                .ToListAsync(cancellationToken);

            var geofences = await db.Geofences.AsNoTracking()
                .Where(g => geofenceIds.Contains(g.Id) && g.OrganizationId == incident.OrganizationId)
                .ToListAsync(cancellationToken);

            foreach (var gf in geofences)
            {
                var gfId = $"Geofence:{gf.Id}";
                if (!nodes.ContainsKey(gfId))
                {
                    nodes[gfId] = new GraphNodeDto(
                        gfId,
                        "Geofence",
                        gf.Id,
                        gf.Name,
                        gf.GeofenceType.ToString(),
                        2);
                }

                edges.Add(new GraphEdgeDto(
                    $"{assetNodeId}->assigned->{gfId}",
                    assetNodeId,
                    gfId,
                    "assigned"));
            }
        }

        var types = edges.Select(e => e.RelationshipType).Distinct().OrderBy(x => x).ToList();
        return AnalysisResult<IncidentGraphDto>.Success(
            new IncidentGraphDto(incident.Id, nodes.Values.ToList(), edges, types));
    }

    private async Task<(IncidentContext? Data, AnalysisError? Error)> LoadContextAsync(
        Guid incidentId,
        CancellationToken cancellationToken)
    {
        var incident = await db.Incidents.AsNoTracking()
            .FirstOrDefaultAsync(
                i => i.Id == incidentId && i.OrganizationId == organizationContext.OrganizationId,
                cancellationToken);

        if (incident is null)
        {
            return (null, new AnalysisError(AnalysisErrorCode.NotFound, "Incident not found."));
        }

        var detections = await db.Detections.AsNoTracking()
            .Where(d => d.IncidentId == incidentId)
            .OrderBy(d => d.TriggeredAt)
            .ToListAsync(cancellationToken);

        var timeline = await db.IncidentTimelineEntries.AsNoTracking()
            .Where(t => t.IncidentId == incidentId)
            .OrderBy(t => t.Timestamp)
            .ToListAsync(cancellationToken);

        var relationships = await db.IncidentEntities.AsNoTracking()
            .Where(e => e.IncidentId == incidentId)
            .ToListAsync(cancellationToken);

        var risk = await db.RiskAssessments.AsNoTracking()
            .Where(r => r.IncidentId == incidentId)
            .OrderByDescending(r => r.CalculatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var anomalies = await db.AnomalyAssessments.AsNoTracking()
            .Where(a => a.IncidentId == incidentId ||
                        (a.AssetId == incident.PrimaryAssetId &&
                         a.CalculatedAt >= incident.StartedAt.AddHours(-1) &&
                         a.CalculatedAt <= (incident.EndedAt ?? DateTimeOffset.UtcNow).AddHours(1)))
            .OrderByDescending(a => a.CalculatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        var asset = await db.Assets.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == incident.PrimaryAssetId, cancellationToken);

        var positionCount = await db.TelemetryEvents.AsNoTracking()
            .CountAsync(
                e => e.AssetId == incident.PrimaryAssetId &&
                     e.RecordedAt >= incident.StartedAt.AddMinutes(-15) &&
                     e.RecordedAt <= (incident.EndedAt ?? incident.DetectedAt).AddMinutes(15),
                cancellationToken);

        return (new IncidentContext(
            incident,
            asset?.Name ?? incident.PrimaryAssetId.ToString(),
            detections,
            timeline,
            relationships,
            risk,
            anomalies,
            positionCount), null);
    }

    private static IncidentSummaryDto BuildSummary(IncidentContext ctx)
    {
        var facts = new List<AnalysisStatementDto>();
        var citations = new List<CitationDto>();

        foreach (var detection in ctx.Detections)
        {
            var sourceId = $"DET-{detection.Id.ToString()[..8].ToUpperInvariant()}";
            var text = $"{detection.Title} at {detection.TriggeredAt.UtcDateTime:HH:mm} UTC.";
            var citation = new CitationDto(text, "Detection", sourceId, detection.Description);
            citations.Add(citation);
            facts.Add(new AnalysisStatementDto("Fact", text, [citation]));
        }

        foreach (var anomaly in ctx.Anomalies.Where(a => a.IsAnomaly).Take(3))
        {
            var sourceId = $"ANO-{anomaly.Id.ToString()[..8].ToUpperInvariant()}";
            var text =
                $"Anomaly score {anomaly.Score:F2} ({anomaly.Method}) — {Truncate(anomaly.Explanation, 160)}";
            var citation = new CitationDto(text, "AnomalyAssessment", sourceId, anomaly.ModelVersion);
            citations.Add(citation);
            facts.Add(new AnalysisStatementDto("Fact", text, [citation]));
        }

        if (facts.Count == 0)
        {
            var text = $"Incident opened at {ctx.Incident.DetectedAt.UtcDateTime:u} with no linked detections yet.";
            var citation = new CitationDto(text, "Incident", $"INC-{ctx.Incident.Id.ToString()[..8].ToUpperInvariant()}", null);
            citations.Add(citation);
            facts.Add(new AnalysisStatementDto("Fact", text, [citation]));
        }

        var suspicions = new List<AnalysisStatementDto>();
        if (ctx.Detections.Any(d => d.DetectionType is DetectionRuleType.GeofenceExit or DetectionRuleType.UnauthorizedUser) ||
            ctx.Incident.IncidentType is IncidentType.PossibleTheft or IncidentType.UnauthorizedUse)
        {
            suspicions.Add(new AnalysisStatementDto(
                "Suspicion",
                "The asset may have been used without authorization.",
                citations.Take(2).ToList()));
        }

        if (ctx.Detections.Any(d => d.DetectionType == DetectionRuleType.GpsOffline))
        {
            suspicions.Add(new AnalysisStatementDto(
                "Suspicion",
                "GPS interruption may indicate signal jamming or deliberate device power-off.",
                citations.Where(c => c.Claim.Contains("GPS", StringComparison.OrdinalIgnoreCase)).ToList()));
        }

        var assumptions = new List<AnalysisStatementDto>
        {
            new(
                "Assumption",
                "Assigned driver identity is inferred from system assignment records when present; physical custody is not independently verified.",
                [])
        };

        var missing = BuildMissingData(ctx).MissingData;
        var summary = BuildNarrativeLead(ctx);

        return new IncidentSummaryDto(
            summary,
            facts,
            suspicions,
            assumptions,
            missing,
            citations,
            AnalystVersion);
    }

    private static RiskExplanationDto BuildRiskExplanation(IncidentContext ctx)
    {
        var factors = new List<AnalysisStatementDto>();
        var citations = new List<CitationDto>();

        if (ctx.Risk is not null)
        {
            try
            {
                using var doc = JsonDocument.Parse(ctx.Risk.Factors);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in doc.RootElement.EnumerateArray())
                    {
                        var label = el.TryGetProperty("label", out var l) ? l.GetString() : "Factor";
                        var points = el.TryGetProperty("points", out var p) ? p.GetInt32() : 0;
                        var factorExplanation = el.TryGetProperty("explanation", out var e) ? e.GetString() : null;
                        var text = $"{label}: +{points} points. {factorExplanation}".Trim();
                        var citation = new CitationDto(
                            text,
                            "RiskAssessment",
                            $"RISK-{ctx.Risk.Id.ToString()[..8].ToUpperInvariant()}",
                            ctx.Risk.ModelVersion);
                        citations.Add(citation);
                        factors.Add(new AnalysisStatementDto("Fact", text, [citation]));
                    }
                }
            }
            catch (JsonException)
            {
                // ignore malformed factors
            }
        }

        foreach (var detection in ctx.Detections)
        {
            var sourceId = $"DET-{detection.Id.ToString()[..8].ToUpperInvariant()}";
            var text = $"{detection.Title} contributed {detection.RiskContribution} risk points.";
            var citation = new CitationDto(text, "Detection", sourceId, null);
            citations.Add(citation);
            factors.Add(new AnalysisStatementDto("Fact", text, [citation]));
        }

        var level = ctx.Risk?.RiskLevel.ToString() ?? ctx.Incident.Severity.ToString();
        var explanation =
            $"Incident risk is {ctx.Incident.RiskScore}/100 ({level}). " +
            "Score is derived from correlated detections and compound factors; " +
            "machine-learning anomaly scores are treated as supporting evidence, not facts.";

        return new RiskExplanationDto(
            ctx.Incident.RiskScore,
            level,
            explanation,
            factors,
            citations,
            AnalystVersion);
    }

    private static MissingDataDto BuildMissingData(IncidentContext ctx)
    {
        var missing = new List<string>();
        var actions = new List<string>();
        var citations = new List<CitationDto>();

        var hasUnauthorized = ctx.Detections.Any(d => d.DetectionType == DetectionRuleType.UnauthorizedUser);
        var hasGeofence = ctx.Detections.Any(d =>
            d.DetectionType is DetectionRuleType.GeofenceEnter or DetectionRuleType.GeofenceExit);
        var hasGpsGap = ctx.Detections.Any(d => d.DetectionType == DetectionRuleType.GpsOffline);

        if (hasGeofence)
        {
            missing.Add("Access log from the site gate / perimeter");
            missing.Add("CCTV or site camera footage covering the departure window");
            actions.Add("Request access-control export for the geofence site");
        }

        if (hasUnauthorized || ctx.Relationships.All(r => r.EntityType != "User"))
        {
            missing.Add("Confirmation from the responsible driver");
            actions.Add("Contact the assigned driver and verify shift status");
        }

        if (hasGpsGap)
        {
            missing.Add("Device diagnostic / power events during GPS outage");
            actions.Add("Inspect device battery and last known cellular attach");
        }

        if (ctx.PositionCount < 3)
        {
            missing.Add("Dense GPS trail for route reconstruction");
            actions.Add("Widen telemetry window or check device reporting interval");
        }

        if (ctx.Incident.AssignedToUserId is null)
        {
            missing.Add("Assigned investigator");
            actions.Add("Assign an investigator on the incident");
        }

        if (missing.Count == 0)
        {
            missing.Add("No critical gaps identified from current system data");
            actions.Add("Continue monitoring live telemetry and update the timeline");
        }

        var citation = new CitationDto(
            "Gap analysis based on detections and timeline completeness.",
            "Incident",
            $"INC-{ctx.Incident.Id.ToString()[..8].ToUpperInvariant()}",
            null);
        citations.Add(citation);

        return new MissingDataDto(missing, actions, citations, AnalystVersion);
    }

    private async Task<SimilarIncidentsDto> SearchSimilarAsync(
        IncidentContext ctx,
        CancellationToken cancellationToken)
    {
        var candidates = await db.Incidents.AsNoTracking()
            .Where(i => i.OrganizationId == ctx.Incident.OrganizationId &&
                        i.Id != ctx.Incident.Id)
            .OrderByDescending(i => i.DetectedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        var scored = candidates
            .Select(i =>
            {
                double similarity = 0;
                var reasons = new List<string>();
                if (i.PrimaryAssetId == ctx.Incident.PrimaryAssetId)
                {
                    similarity += 0.45;
                    reasons.Add("same asset");
                }

                if (i.IncidentType == ctx.Incident.IncidentType)
                {
                    similarity += 0.25;
                    reasons.Add("same incident type");
                }

                var riskDelta = Math.Abs(i.RiskScore - ctx.Incident.RiskScore);
                if (riskDelta <= 15)
                {
                    similarity += 0.15;
                    reasons.Add("similar risk score");
                }

                var hoursApart = Math.Abs((i.DetectedAt - ctx.Incident.DetectedAt).TotalHours);
                if (hoursApart <= 72)
                {
                    similarity += 0.15;
                    reasons.Add("within 72 hours");
                }

                return new { Incident = i, Similarity = similarity, Reason = string.Join(", ", reasons) };
            })
            .Where(x => x.Similarity >= 0.25)
            .OrderByDescending(x => x.Similarity)
            .Take(5)
            .Select(x => new SimilarIncidentDto(
                x.Incident.Id,
                x.Incident.Title,
                x.Incident.IncidentType.ToString(),
                x.Incident.RiskScore,
                x.Incident.Status.ToString(),
                x.Incident.DetectedAt,
                Math.Round(x.Similarity, 2),
                x.Reason))
            .ToList();

        var citations = scored.Select(s => new CitationDto(
            $"Similar incident '{s.Title}' (similarity {s.Similarity:F2}).",
            "Incident",
            $"INC-{s.IncidentId.ToString()[..8].ToUpperInvariant()}",
            s.Reason)).ToList();

        return new SimilarIncidentsDto(scored, citations, AnalystVersion);
    }

    private static string BuildNarrativeLead(IncidentContext ctx)
    {
        var detectionBits = ctx.Detections
            .Select(d => d.Title.ToLowerInvariant())
            .Distinct()
            .Take(4)
            .ToList();

        var sb = new StringBuilder();
        sb.Append(ctx.AssetName);
        sb.Append(" triggered incident '");
        sb.Append(ctx.Incident.Title);
        sb.Append("' at ");
        sb.Append(ctx.Incident.DetectedAt.UtcDateTime.ToString("u"));
        sb.Append(" UTC with risk score ");
        sb.Append(ctx.Incident.RiskScore);
        sb.Append("/100.");

        if (detectionBits.Count > 0)
        {
            sb.Append(" Linked alerts: ");
            sb.Append(string.Join("; ", detectionBits));
            sb.Append('.');
        }

        return sb.ToString();
    }

    private static string BuildNarrative(
        IncidentContext ctx,
        IncidentSummaryDto analysis,
        RiskExplanationDto risk)
    {
        var sb = new StringBuilder();
        sb.AppendLine(analysis.Summary);
        sb.AppendLine();
        sb.AppendLine(risk.Explanation);
        sb.AppendLine();
        sb.AppendLine("Facts:");
        foreach (var fact in analysis.Facts)
        {
            var cite = fact.Citations.FirstOrDefault();
            sb.Append("- ");
            sb.Append(fact.Text);
            if (cite is not null)
            {
                sb.Append(" [");
                sb.Append(cite.SourceType);
                sb.Append(' ');
                sb.Append(cite.SourceId);
                sb.Append(']');
            }

            sb.AppendLine();
        }

        if (analysis.Suspicions.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Suspicions (not established facts):");
            foreach (var s in analysis.Suspicions)
            {
                sb.Append("- ");
                sb.AppendLine(s.Text);
            }
        }

        if (analysis.Assumptions.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Assumptions:");
            foreach (var a in analysis.Assumptions)
            {
                sb.Append("- ");
                sb.AppendLine(a.Text);
            }
        }

        if (analysis.MissingData.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Missing data:");
            foreach (var m in analysis.MissingData)
            {
                sb.Append("- ");
                sb.AppendLine(m);
            }
        }

        return sb.ToString().TrimEnd();
    }

    private async Task<string> ResolveLabelAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        return entityType switch
        {
            "Asset" => await db.Assets.AsNoTracking()
                .Where(a => a.Id == entityId)
                .Select(a => a.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? entityId.ToString()[..8],
            "User" => await db.Users.AsNoTracking()
                .Where(u => u.Id == entityId)
                .Select(u => u.FirstName + " " + u.LastName)
                .FirstOrDefaultAsync(cancellationToken) ?? entityId.ToString()[..8],
            "Geofence" => await db.Geofences.AsNoTracking()
                .Where(g => g.Id == entityId)
                .Select(g => g.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? entityId.ToString()[..8],
            "Detection" => await db.Detections.AsNoTracking()
                .Where(d => d.Id == entityId)
                .Select(d => d.Title)
                .FirstOrDefaultAsync(cancellationToken) ?? entityId.ToString()[..8],
            _ => entityId.ToString()[..8]
        };
    }

    private void LogTool(string tool, Guid incidentId)
    {
        logger.LogInformation(
            "Agent tool {Tool} org={OrgId} user={UserId} incident={IncidentId}",
            tool,
            organizationContext.OrganizationId,
            organizationContext.UserId,
            incidentId);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..(max - 1)] + "…";

    private sealed record IncidentContext(
        Incident Incident,
        string AssetName,
        IReadOnlyList<Domain.Detections.Detection> Detections,
        IReadOnlyList<IncidentTimelineEntry> Timeline,
        IReadOnlyList<IncidentEntity> Relationships,
        RiskAssessment? Risk,
        IReadOnlyList<Domain.Anomaly.AnomalyAssessment> Anomalies,
        int PositionCount);
}

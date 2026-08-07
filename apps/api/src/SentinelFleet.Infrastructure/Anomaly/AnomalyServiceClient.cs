using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SentinelFleet.Application.Anomaly;

namespace SentinelFleet.Infrastructure.Anomaly;

public sealed class AnomalyServiceOptions
{
    public const string SectionName = "AnomalyService";

    public string BaseUrl { get; set; } = "http://localhost:8090";

    public int TimeoutSeconds { get; set; } = 5;
}

public sealed class AnomalyServiceClient(
    HttpClient httpClient,
    ILogger<AnomalyServiceClient> logger) : IAnomalyServiceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Anomaly service health check failed");
            return false;
        }
    }

    public async Task<AnomalyScoreResult?> ScoreAsync(
        AnomalyScoreRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                organization_id = request.OrganizationId.ToString(),
                asset_id = request.AssetId.ToString(),
                event_id = request.EventId,
                recorded_at = request.RecordedAt?.ToString("O"),
                features = new
                {
                    hour_of_day = request.Features.HourOfDay,
                    day_of_week = request.Features.DayOfWeek,
                    speed_kph = request.Features.SpeedKph,
                    ignition_on = request.Features.IgnitionOn,
                    fuel_level_percent = request.Features.FuelLevelPercent,
                    odometer_km = request.Features.OdometerKm
                }
            };

            using var response = await httpClient.PostAsJsonAsync(
                "/v1/score",
                payload,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Anomaly service returned {StatusCode} for asset {AssetId}",
                    (int)response.StatusCode,
                    request.AssetId);
                return null;
            }

            var body = await response.Content.ReadFromJsonAsync<AnomalyScoreApiResponse>(
                JsonOptions,
                cancellationToken);

            if (body is null)
            {
                return null;
            }

            return new AnomalyScoreResult(
                body.AnomalyScore,
                body.Confidence,
                body.ModelVersion,
                body.FeaturesUsed ?? [],
                body.Explanation,
                body.IsAnomaly,
                body.Method);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to score anomaly for asset {AssetId}", request.AssetId);
            return null;
        }
    }

    private sealed record AnomalyScoreApiResponse(
        [property: JsonPropertyName("anomaly_score")] double AnomalyScore,
        [property: JsonPropertyName("confidence")] double Confidence,
        [property: JsonPropertyName("model_version")] string ModelVersion,
        [property: JsonPropertyName("features_used")] List<string>? FeaturesUsed,
        [property: JsonPropertyName("explanation")] string Explanation,
        [property: JsonPropertyName("is_anomaly")] bool IsAnomaly,
        [property: JsonPropertyName("method")] string Method);
}

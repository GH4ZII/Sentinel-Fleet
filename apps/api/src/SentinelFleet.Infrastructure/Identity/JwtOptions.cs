namespace SentinelFleet.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "sentinel-fleet";

    public string Audience { get; set; } = "sentinel-fleet";

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 7;
}

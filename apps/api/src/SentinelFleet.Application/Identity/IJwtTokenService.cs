using SentinelFleet.Domain.Identity;

namespace SentinelFleet.Application.Identity;

public interface IJwtTokenService
{
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user);

    (string PlaintextToken, string TokenHash, DateTimeOffset ExpiresAt) CreateRefreshToken();

    string HashRefreshToken(string plaintextToken);
}

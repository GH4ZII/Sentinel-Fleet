using SentinelFleet.Domain.Identity;
using SentinelFleet.Domain.Organizations;

namespace SentinelFleet.Application.Identity;

public interface IJwtTokenService
{
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(
        User user,
        Guid organizationId,
        OrganizationRole role);

    (string PlaintextToken, string TokenHash, DateTimeOffset ExpiresAt) CreateRefreshToken();

    string HashRefreshToken(string plaintextToken);
}

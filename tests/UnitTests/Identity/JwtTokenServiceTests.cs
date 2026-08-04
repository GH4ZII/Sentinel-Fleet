using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using SentinelFleet.Domain.Identity;
using SentinelFleet.Infrastructure.Identity;

namespace SentinelFleet.UnitTests.Identity;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _sut = new(Options.Create(new JwtOptions
    {
        Issuer = "sentinel-fleet",
        Audience = "sentinel-fleet",
        SigningKey = "unit_test_signing_key_at_least_32_chars!",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7
    }));

    [Fact]
    public void CreateAccessToken_ContainsExpectedClaims()
    {
        var user = new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Email = "ada@example.com",
            FirstName = "Ada",
            LastName = "Lovelace",
            PasswordHash = "hash",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var (token, expiresAt) = _sut.CreateAccessToken(user);

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.True(expiresAt > DateTimeOffset.UtcNow);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(user.Id.ToString(), jwt.Subject);
        Assert.Equal(user.Email, jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("Ada Lovelace", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Name).Value);
        Assert.Equal("sentinel-fleet", jwt.Issuer);
        Assert.Contains("sentinel-fleet", jwt.Audiences);
    }

    [Fact]
    public void CreateRefreshToken_HashIsDeterministicAndNotPlaintext()
    {
        var (plaintext, hash, expiresAt) = _sut.CreateRefreshToken();

        Assert.False(string.IsNullOrWhiteSpace(plaintext));
        Assert.NotEqual(plaintext, hash);
        Assert.Equal(hash, _sut.HashRefreshToken(plaintext));
        Assert.True(expiresAt > DateTimeOffset.UtcNow.AddDays(6));
    }
}

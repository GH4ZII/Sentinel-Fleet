using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SentinelFleet.Application.Identity;
using SentinelFleet.Domain.Identity;
using SentinelFleet.Infrastructure.Identity;
using SentinelFleet.Infrastructure.Persistence;

namespace SentinelFleet.UnitTests.Identity;

public class AuthServiceRefreshTests
{
    [Fact]
    public async Task RefreshAsync_RotatesToken_AndRevokesOld()
    {
        await using var db = CreateDbContext();
        var tokenService = CreateTokenService();
        var hasher = new PasswordHasher<User>();
        var sut = new AuthService(db, tokenService, hasher);

        var register = await sut.RegisterAsync(new RegisterRequest(
            "refresh@example.com",
            "Password1!",
            "Refresh",
            "User"));

        Assert.True(register.Succeeded);
        var oldRefresh = register.Value!.RefreshToken;

        var refresh = await sut.RefreshAsync(new RefreshRequest(oldRefresh));
        Assert.True(refresh.Succeeded);
        Assert.NotEqual(oldRefresh, refresh.Value!.RefreshToken);

        var storedTokens = await db.RefreshTokens.AsNoTracking().ToListAsync();
        Assert.Equal(2, storedTokens.Count);

        var revoked = storedTokens.Single(t => t.RevokedAt is not null);
        var active = storedTokens.Single(t => t.RevokedAt is null);
        Assert.Equal(active.Id, revoked.ReplacedByTokenId);

        var reuse = await sut.RefreshAsync(new RefreshRequest(oldRefresh));
        Assert.False(reuse.Succeeded);
        Assert.Equal(AuthErrorCode.Unauthorized, reuse.Error!.Code);
    }

    [Fact]
    public async Task LogoutAsync_RevokesRefreshToken()
    {
        await using var db = CreateDbContext();
        var tokenService = CreateTokenService();
        var hasher = new PasswordHasher<User>();
        var sut = new AuthService(db, tokenService, hasher);

        var login = await sut.RegisterAsync(new RegisterRequest(
            "logout@example.com",
            "Password1!",
            "Log",
            "Out"));

        Assert.True(login.Succeeded);
        var refreshToken = login.Value!.RefreshToken;

        var logout = await sut.LogoutAsync(new LogoutRequest(refreshToken));
        Assert.True(logout.Succeeded);

        var refresh = await sut.RefreshAsync(new RefreshRequest(refreshToken));
        Assert.False(refresh.Succeeded);
    }

    private static SentinelFleetDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SentinelFleetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SentinelFleetDbContext(options);
    }

    private static JwtTokenService CreateTokenService() =>
        new(Options.Create(new JwtOptions
        {
            Issuer = "sentinel-fleet",
            Audience = "sentinel-fleet",
            SigningKey = "unit_test_signing_key_at_least_32_chars!",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        }));
}

using Microsoft.AspNetCore.Identity;
using SentinelFleet.Domain.Identity;

namespace SentinelFleet.UnitTests.Identity;

public class PasswordHasherTests
{
    private readonly PasswordHasher<User> _hasher = new();

    [Fact]
    public void HashPassword_CanBeVerified()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var hash = _hasher.HashPassword(user, "CorrectHorseBattery");
        user.PasswordHash = hash;

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, "CorrectHorseBattery");
        Assert.Equal(PasswordVerificationResult.Success, result);
    }

    [Fact]
    public void VerifyHashedPassword_FailsForWrongPassword()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow
        };

        user.PasswordHash = _hasher.HashPassword(user, "CorrectHorseBattery");

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, "wrong-password");
        Assert.Equal(PasswordVerificationResult.Failed, result);
    }
}

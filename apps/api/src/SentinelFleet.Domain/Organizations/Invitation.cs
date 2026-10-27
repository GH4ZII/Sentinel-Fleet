using SentinelFleet.Domain.Identity;

namespace SentinelFleet.Domain.Organizations;

public sealed class Invitation
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; } = null!;

    public required string Email { get; set; }

    public OrganizationRole Role { get; set; }

    public required string Token { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? AcceptedAt { get; set; }

    public Guid CreatedByUserId { get; set; }

    public User CreatedByUser { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
}

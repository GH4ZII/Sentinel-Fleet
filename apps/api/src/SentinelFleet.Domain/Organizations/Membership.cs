using SentinelFleet.Domain.Identity;

namespace SentinelFleet.Domain.Organizations;

public sealed class Membership
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public OrganizationRole Role { get; set; }

    public MembershipStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

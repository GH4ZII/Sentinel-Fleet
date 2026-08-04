namespace SentinelFleet.Domain.Organizations;

public sealed class Organization
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? OrganizationNumber { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string Settings { get; set; } = "{}";

    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
}

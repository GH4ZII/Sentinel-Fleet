namespace SentinelFleet.Domain.Drivers;

public sealed class DriverAssignment
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid AssetId { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset ValidFrom { get; set; }

    public DateTimeOffset? ValidTo { get; set; }

    public string AssignmentType { get; set; } = "Primary";
}

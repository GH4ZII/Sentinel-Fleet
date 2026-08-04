namespace SentinelFleet.Domain.Assets;

public sealed class AssetType
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public required string Name { get; set; }

    public string? Icon { get; set; }

    public string? Description { get; set; }

    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
}

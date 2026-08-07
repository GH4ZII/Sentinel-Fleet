namespace SentinelFleet.Application.Incidents;

public interface IAttachmentStorage
{
    Task<string> SaveAsync(
        Guid organizationId,
        Guid incidentId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
}

public sealed class AttachmentStorageOptions
{
    public const string SectionName = "AttachmentStorage";

    public string RootPath { get; set; } = "data/attachments";
}

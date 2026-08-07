using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SentinelFleet.Application.Incidents;

namespace SentinelFleet.Infrastructure.Incidents;

public sealed class LocalAttachmentStorage(
    IOptions<AttachmentStorageOptions> options,
    ILogger<LocalAttachmentStorage> logger) : IAttachmentStorage
{
    public async Task<string> SaveAsync(
        Guid organizationId,
        Guid incidentId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var root = Path.GetFullPath(options.Value.RootPath);
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "attachment.bin";
        }

        var key = $"{organizationId:N}/{incidentId:N}/{Guid.NewGuid():N}_{safeName}";
        var fullPath = Path.Combine(root, key.Replace('/', Path.DirectorySeparatorChar));
        var directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);

        await using var file = File.Create(fullPath);
        await content.CopyToAsync(file, cancellationToken);

        logger.LogInformation("Stored attachment at {StorageKey}", key);
        return key;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var root = Path.GetFullPath(options.Value.RootPath);
        var fullPath = Path.GetFullPath(
            Path.Combine(root, storageKey.Replace('/', Path.DirectorySeparatorChar)));

        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
        {
            throw new FileNotFoundException("Attachment file not found.", storageKey);
        }

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }
}

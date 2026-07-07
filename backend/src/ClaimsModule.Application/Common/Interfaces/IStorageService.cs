namespace ClaimsModule.Application.Common.Interfaces;

/// <summary>
/// Document storage abstraction (BR-D-01/02/03) - AzureBlobStorageService and
/// LocalFileSystemStorageService both live in Infrastructure; provider is chosen via
/// appsettings.json (Storage:Provider).
/// </summary>
public interface IStorageService
{
    Task UploadAsync(string blobPath, Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Short-lived download URL (1h TTL per BR-D-02); bytes are never proxied through the API.</summary>
    Task<string> GetDownloadUrlAsync(string blobPath, TimeSpan ttl, CancellationToken cancellationToken = default);
}

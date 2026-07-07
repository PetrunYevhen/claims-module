using ClaimsModule.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace ClaimsModule.Infrastructure.Storage;

/// <summary>
/// Local dev fallback (BR-D-03). Files are written under RootPath and served back by ASP.NET
/// Core's static file middleware mounted at /uploads (see Program.cs) - there is no real SAS/TTL
/// mechanism here, unlike the Azure implementation.
/// </summary>
public class LocalFileSystemStorageService(IOptions<LocalStorageOptions> options) : IStorageService
{
    public async Task UploadAsync(string blobPath, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(options.Value.RootPath, blobPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, cancellationToken);
    }

    public Task<string> GetDownloadUrlAsync(string blobPath, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var relativeUrl = $"/uploads/{blobPath.Replace('\\', '/')}";
        return Task.FromResult(relativeUrl);
    }
}

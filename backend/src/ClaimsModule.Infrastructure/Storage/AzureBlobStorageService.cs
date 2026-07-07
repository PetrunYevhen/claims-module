using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using ClaimsModule.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace ClaimsModule.Infrastructure.Storage;

public class AzureBlobStorageService(BlobServiceClient blobServiceClient, IOptions<AzureBlobOptions> options) : IStorageService
{
    public async Task UploadAsync(string blobPath, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(options.Value.ContainerName);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blob = container.GetBlobClient(blobPath);
        await blob.UploadAsync(
            content,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: cancellationToken);
    }

    public Task<string> GetDownloadUrlAsync(string blobPath, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(options.Value.ContainerName);
        var blob = container.GetBlobClient(blobPath);

        if (!blob.CanGenerateSasUri)
        {
            throw new InvalidOperationException(
                "Blob client cannot generate SAS URIs - the storage account must be configured with shared key credentials.");
        }

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = options.Value.ContainerName,
            BlobName = blobPath,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(ttl)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        return Task.FromResult(blob.GenerateSasUri(sasBuilder).ToString());
    }
}

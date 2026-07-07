using ClaimsModule.Domain.Common;

namespace ClaimsModule.Domain.Entities;

public class ClaimDocument : AuditableEntity
{
    public Guid ClaimDocumentId { get; set; }
    public Guid ClaimId { get; set; }
    public string DocumentType { get; set; } = null!;
    public string DocumentName { get; set; } = null!;
    public string BlobPath { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long FileSizeBytes { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public string? Notes { get; set; }

    public Claim Claim { get; set; } = null!;
}

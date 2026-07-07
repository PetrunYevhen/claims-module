namespace ClaimsModule.Application.Common.Interfaces;

/// <summary>
/// Sole write path to ClaimAuditLog (BR-A-01: no direct DbContext writes from handlers).
/// Entries are staged on the tracked IClaimsDbContext and persisted by the caller's
/// SaveChangesAsync, so a log entry is only ever committed atomically with the change it describes.
/// </summary>
public interface IAuditLogService
{
    Task LogAsync(
        Guid claimId,
        string eventType,
        string description,
        object? oldValue = null,
        object? newValue = null,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken cancellationToken = default);
}

using ClaimsModule.Domain.Common;

namespace ClaimsModule.Domain.Entities;

/// <summary>
/// Immutable append-only event log. No UPDATE or DELETE is ever performed - see BR-A-01.
/// </summary>
public class ClaimAuditLog : AppendOnlyEntity
{
    public Guid AuditLogId { get; set; }
    public Guid ClaimId { get; set; }
    public string EventType { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? CorrelationId { get; set; }
    public Guid? CreatedByUserId { get; set; }

    public Claim Claim { get; set; } = null!;
}

using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Domain.Entities;

public class Claim : AuditableEntity
{
    public Guid ClaimId { get; set; }
    public string ClaimNumber { get; set; } = null!;
    public Guid? PolicyId { get; set; }
    public string? PolicyNumber { get; set; }
    public string? ClientName { get; set; }
    public ClaimStatus Status { get; set; }
    public ClaimSeverity? Severity { get; set; }
    public DateTimeOffset ReportedDate { get; set; }
    public Guid? AssignedHandlerId { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public string? ClosureReason { get; set; }
    public string? Notes { get; set; }

    /// <summary>Manager-set flag permitting reserve approval past the $10M aggregate cap (BR-R-05).</summary>
    public bool AggregateLimitOverride { get; set; }

    public byte[] RowVer { get; set; } = null!;

    public LossEvent? LossEvent { get; set; }
    public ICollection<ClaimParty> Parties { get; set; } = new List<ClaimParty>();
    public ICollection<ClaimRiskObject> RiskObjects { get; set; } = new List<ClaimRiskObject>();
    public ICollection<ClaimReserveComponent> ReserveComponents { get; set; } = new List<ClaimReserveComponent>();
    public ICollection<ClaimDocument> Documents { get; set; } = new List<ClaimDocument>();
    public ICollection<ClaimAuditLog> AuditLogEntries { get; set; } = new List<ClaimAuditLog>();
}

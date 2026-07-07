using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Domain.Entities;

public class ClaimRiskObject : AuditableEntity
{
    public Guid ClaimRiskObjectId { get; set; }
    public Guid ClaimId { get; set; }
    public AssetType AssetType { get; set; }
    public string AssetDescription { get; set; } = null!;
    public string? DamageDescription { get; set; }
    public bool IsPrimary { get; set; }
    public string? AssetReference { get; set; }

    public Claim Claim { get; set; } = null!;
}

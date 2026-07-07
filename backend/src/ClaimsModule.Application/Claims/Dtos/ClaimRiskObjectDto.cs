using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Application.Claims.Dtos;

public class ClaimRiskObjectDto
{
    public Guid ClaimRiskObjectId { get; set; }
    public AssetType AssetType { get; set; }
    public string AssetDescription { get; set; } = null!;
    public string? DamageDescription { get; set; }
    public bool IsPrimary { get; set; }
    public string? AssetReference { get; set; }
}

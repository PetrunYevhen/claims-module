using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Domain.Entities;

public class ClaimParty : AuditableEntity
{
    public Guid ClaimPartyId { get; set; }
    public Guid ClaimId { get; set; }
    public PartyRole PartyRole { get; set; }
    public PartyType PartyType { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? CompanyName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public Claim Claim { get; set; } = null!;
}

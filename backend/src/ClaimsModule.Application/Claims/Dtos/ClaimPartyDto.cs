using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Application.Claims.Dtos;

public class ClaimPartyDto
{
    public Guid ClaimPartyId { get; set; }
    public PartyRole PartyRole { get; set; }
    public PartyType PartyType { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? CompanyName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
}

using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Application.Claims.Dtos;

public class ClaimSummaryDto
{
    public Guid ClaimId { get; set; }
    public string ClaimNumber { get; set; } = null!;
    public string? PolicyNumber { get; set; }
    public string? ClientName { get; set; }
    public DateTimeOffset? LossDate { get; set; }
    public string? CauseOfLossCode { get; set; }
    public ClaimStatus Status { get; set; }
    public decimal TotalReserves { get; set; }
}

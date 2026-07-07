using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Application.Claims.Dtos;

public class ClaimDetailDto
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

    public LossEventDto? LossEvent { get; set; }
    public List<ClaimPartyDto> Parties { get; set; } = [];
    public List<ClaimRiskObjectDto> RiskObjects { get; set; } = [];
    public List<ValidationIssueDto> ValidationIssues { get; set; } = [];
    public List<string> ValidNextStatuses { get; set; } = [];
}

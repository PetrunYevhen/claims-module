using ClaimsModule.Domain.Common;

namespace ClaimsModule.Domain.Entities;

public class LossEvent : AuditableEntity
{
    public Guid LossEventId { get; set; }
    public Guid ClaimId { get; set; }
    public DateTimeOffset LossDate { get; set; }
    public string LossDescription { get; set; } = null!;
    public string? LossLocation { get; set; }
    public string CauseOfLossCode { get; set; } = null!;
    public decimal? EstimatedLossAmount { get; set; }
    public DateTimeOffset ReportDate { get; set; }
    public string? PoliceReportNumber { get; set; }

    public Claim Claim { get; set; } = null!;
}

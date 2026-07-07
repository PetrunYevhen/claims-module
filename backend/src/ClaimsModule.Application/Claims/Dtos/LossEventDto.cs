namespace ClaimsModule.Application.Claims.Dtos;

public class LossEventDto
{
    public Guid LossEventId { get; set; }
    public DateTimeOffset LossDate { get; set; }
    public string LossDescription { get; set; } = null!;
    public string? LossLocation { get; set; }
    public string CauseOfLossCode { get; set; } = null!;
    public decimal? EstimatedLossAmount { get; set; }
    public DateTimeOffset ReportDate { get; set; }
    public string? PoliceReportNumber { get; set; }
}

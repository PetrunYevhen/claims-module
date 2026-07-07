namespace ClaimsModule.Application.Reserves.Dtos;

public class ReserveSummaryDto
{
    public List<ReserveComponentDto> Components { get; set; } = [];
    public List<ReserveHistoryDto> History { get; set; } = [];
    public decimal TotalApproved { get; set; }
    public bool AggregateLimitOverride { get; set; }
}

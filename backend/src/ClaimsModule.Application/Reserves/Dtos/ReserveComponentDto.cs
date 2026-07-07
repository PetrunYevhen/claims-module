using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Application.Reserves.Dtos;

public class ReserveComponentDto
{
    public Guid ReserveComponentId { get; set; }
    public ReserveComponentType Component { get; set; }
    public decimal CurrentAmount { get; set; }
    public ReserveComponentStatus Status { get; set; }
    public decimal PendingAmount { get; set; }
}

using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Domain.Entities;

public class ClaimReserveComponent : AuditableEntity
{
    public Guid ReserveComponentId { get; set; }
    public Guid ClaimId { get; set; }
    public ReserveComponentType Component { get; set; }

    /// <summary>Denormalised cache of the sum of all posted/approved ReserveHistory transactions.</summary>
    public decimal CurrentAmount { get; set; }
    public ReserveComponentStatus Status { get; set; }
    public string? Notes { get; set; }
    public byte[] RowVer { get; set; } = null!;

    public Claim Claim { get; set; } = null!;
    public ICollection<ReserveHistory> History { get; set; } = new List<ReserveHistory>();
}

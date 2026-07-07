using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Domain.Entities;

/// <summary>Reference/lookup table seeded at migration time - see FRS 5.6.</summary>
public class CauseOfLossCode
{
    public Guid CauseOfLossCodeId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public PerilCategory PerilCategory { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Domain.Entities;

/// <summary>Simulated policy table seeded at migration time - see FRS 5.5. Stands in for a real Policy module.</summary>
public class Policy
{
    public Guid PolicyId { get; set; }
    public string PolicyNumber { get; set; } = null!;
    public string ClientName { get; set; } = null!;
    public DateOnly EffectiveDate { get; set; }
    public DateOnly ExpirationDate { get; set; }
    public PolicyStatus Status { get; set; }

    /// <summary>Comma-separated list of coverage type names (e.g. "Vehicle,Cargo") - see FRS 5.5.</summary>
    public string CoverageTypes { get; set; } = null!;
}

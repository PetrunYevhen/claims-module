namespace ClaimsModule.Domain.Entities;

/// <summary>
/// Backing table for atomic, per-year claim number generation (BR-C-04).
/// Incremented via UPDATE ... OUTPUT INSERTED inside the claim-creation transaction.
/// </summary>
public class ClaimNumberCounter
{
    public int Year { get; set; }
    public int LastSequence { get; set; }
}

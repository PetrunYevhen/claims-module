using ClaimsModule.Domain.Entities;

namespace ClaimsModule.Application.Common.Interfaces;

public enum ValidationSeverity
{
    Warning,
    Critical
}

public record ValidationIssue(string Code, ValidationSeverity Severity, string Message);

/// <summary>
/// Evaluates FRS section 7 business rules (BR-C-01/02/03/05/06/07) against an already-loaded
/// claim graph. Deliberately stateless/pure - issues are computed on demand (at creation, and
/// again whenever a status transition is attempted) rather than persisted as mutable rows, so
/// there is no separate "unresolved issue" table to keep in sync with the claim's real state.
/// </summary>
public interface IClaimValidationService
{
    IReadOnlyList<ValidationIssue> Evaluate(
        Claim claim,
        LossEvent? lossEvent,
        Policy? policy,
        CauseOfLossCode? causeOfLossCode,
        IEnumerable<ClaimParty> parties);
}

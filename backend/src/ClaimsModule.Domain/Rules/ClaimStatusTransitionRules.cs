using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Domain.Rules;

/// <summary>Claim status state machine - FRS section 4.2. Pure domain logic, no dependencies.</summary>
public static class ClaimStatusTransitionRules
{
    private static readonly Dictionary<ClaimStatus, ClaimStatus[]> Transitions = new()
    {
        [ClaimStatus.Draft] = [ClaimStatus.Open],
        [ClaimStatus.Open] = [ClaimStatus.UnderInvestigation, ClaimStatus.PendingPayment, ClaimStatus.Closed, ClaimStatus.Withdrawn],
        [ClaimStatus.UnderInvestigation] = [ClaimStatus.Open, ClaimStatus.PendingPayment, ClaimStatus.Closed, ClaimStatus.Withdrawn],
        [ClaimStatus.PendingPayment] = [ClaimStatus.Closed],
        [ClaimStatus.Closed] = [ClaimStatus.Reopened],
        // Reopened immediately cascades to Open (FRS 4.2) - modeled as a valid transition so the
        // handler can apply it as an explicit second step and log both events.
        [ClaimStatus.Reopened] = [ClaimStatus.Open],
        [ClaimStatus.Withdrawn] = []
    };

    public static IReadOnlyList<ClaimStatus> GetValidNextStatuses(ClaimStatus from) =>
        Transitions.TryGetValue(from, out var next) ? next : [];

    public static bool IsValidTransition(ClaimStatus from, ClaimStatus to) =>
        GetValidNextStatuses(from).Contains(to);
}

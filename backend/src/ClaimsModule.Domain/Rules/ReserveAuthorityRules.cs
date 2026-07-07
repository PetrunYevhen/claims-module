using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Domain.Rules;

/// <summary>Reserve authority thresholds - FRS 6.3 / BR-R-02. Pure domain logic, no dependencies.</summary>
public static class ReserveAuthorityRules
{
    public const decimal AutoApproveLimit = 10_000m;
    public const decimal SupervisorLimit = 100_000m;
    public const decimal AggregateClaimLimit = 10_000_000m;

    /// <summary>The transaction amount tier determines who may approve it, evaluated on |amount|.</summary>
    public static ReserveApprovalStatus DetermineInitialStatus(decimal amount)
    {
        var magnitude = Math.Abs(amount);
        return magnitude <= AutoApproveLimit
            ? ReserveApprovalStatus.AutoApproved
            : ReserveApprovalStatus.PendingApproval;
    }

    /// <summary>Whether the given role may approve/reject a pending transaction of this amount.</summary>
    public static bool RoleCanApprove(string role, decimal amount)
    {
        var magnitude = Math.Abs(amount);

        if (magnitude <= AutoApproveLimit)
        {
            return true; // any role - though these never reach PendingApproval in practice
        }

        if (magnitude <= SupervisorLimit)
        {
            return role is "supervisor" or "manager";
        }

        return role == "manager";
    }
}

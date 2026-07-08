using ClaimsModule.Domain.Enums;
using ClaimsModule.Domain.Rules;
using Xunit;

namespace ClaimsModule.Tests.Domain;

public class ReserveAuthorityRulesTests
{
    [Theory]
    [InlineData(5_000, ReserveApprovalStatus.AutoApproved)]     // BR-R-02: <= 10k auto
    [InlineData(10_000, ReserveApprovalStatus.AutoApproved)]    // boundary inclusive
    [InlineData(10_000.01, ReserveApprovalStatus.PendingApproval)]
    [InlineData(150_000, ReserveApprovalStatus.PendingApproval)]
    [InlineData(-8_000, ReserveApprovalStatus.AutoApproved)]    // magnitude 8k (subrogation recoverable)
    public void DetermineInitialStatus_uses_the_absolute_amount_tier(decimal amount, ReserveApprovalStatus expected)
    {
        Assert.Equal(expected, ReserveAuthorityRules.DetermineInitialStatus(amount));
    }

    [Theory]
    // <= 10k: any role may approve (never actually reaches the queue, but the rule is permissive)
    [InlineData("handler", 5_000, true)]
    // 10k-100k: supervisor or manager
    [InlineData("handler", 50_000, false)]
    [InlineData("supervisor", 50_000, true)]
    [InlineData("manager", 50_000, true)]
    // > 100k: manager only
    [InlineData("supervisor", 150_000, false)]
    [InlineData("manager", 150_000, true)]
    public void RoleCanApprove_enforces_the_three_tier_authority(string role, decimal amount, bool expected)
    {
        Assert.Equal(expected, ReserveAuthorityRules.RoleCanApprove(role, amount));
    }
}

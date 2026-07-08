using ClaimsModule.Domain.Enums;
using ClaimsModule.Domain.Rules;
using Xunit;

namespace ClaimsModule.Tests.Domain;

public class ClaimStatusTransitionRulesTests
{
    [Theory]
    [InlineData(ClaimStatus.Draft, ClaimStatus.Open)]
    [InlineData(ClaimStatus.Open, ClaimStatus.UnderInvestigation)]
    [InlineData(ClaimStatus.Open, ClaimStatus.PendingPayment)]
    [InlineData(ClaimStatus.Open, ClaimStatus.Closed)]
    [InlineData(ClaimStatus.Open, ClaimStatus.Withdrawn)]
    [InlineData(ClaimStatus.UnderInvestigation, ClaimStatus.Open)]
    [InlineData(ClaimStatus.PendingPayment, ClaimStatus.Closed)]
    [InlineData(ClaimStatus.Closed, ClaimStatus.Reopened)]
    [InlineData(ClaimStatus.Reopened, ClaimStatus.Open)]
    public void IsValidTransition_allows_the_state_machine_edges(ClaimStatus from, ClaimStatus to)
    {
        Assert.True(ClaimStatusTransitionRules.IsValidTransition(from, to));
    }

    [Theory]
    [InlineData(ClaimStatus.Draft, ClaimStatus.Closed)]      // FRS 4.2: no Draft -> Closed shortcut
    [InlineData(ClaimStatus.Draft, ClaimStatus.PendingPayment)]
    [InlineData(ClaimStatus.Closed, ClaimStatus.Open)]        // must go via Reopened
    [InlineData(ClaimStatus.PendingPayment, ClaimStatus.Open)]
    [InlineData(ClaimStatus.Open, ClaimStatus.Reopened)]
    public void IsValidTransition_rejects_transitions_not_in_the_state_machine(ClaimStatus from, ClaimStatus to)
    {
        Assert.False(ClaimStatusTransitionRules.IsValidTransition(from, to));
    }

    [Fact]
    public void Withdrawn_is_terminal()
    {
        Assert.Empty(ClaimStatusTransitionRules.GetValidNextStatuses(ClaimStatus.Withdrawn));
    }

    [Fact]
    public void GetValidNextStatuses_for_Open_matches_the_spec()
    {
        var next = ClaimStatusTransitionRules.GetValidNextStatuses(ClaimStatus.Open);

        Assert.Equal(
            new[] { ClaimStatus.UnderInvestigation, ClaimStatus.PendingPayment, ClaimStatus.Closed, ClaimStatus.Withdrawn }
                .OrderBy(s => s),
            next.OrderBy(s => s));
    }
}

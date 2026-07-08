using ClaimsModule.Application.Claims.Commands.TransitionClaimStatus;
using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Services;
using ClaimsModule.Application.Reserves.Common;
using ClaimsModule.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClaimsModule.Tests.Application;

public class TransitionClaimStatusCommandHandlerTests
{
    private readonly RecordingAuditLog _audit = new();

    private TransitionClaimStatusCommandHandler BuildHandler(
        ClaimsModule.Persistence.ClaimsDbContext context, FakeCurrentUser user)
        => new(context, user, new ClaimValidationService(), _audit, MapperFactory.Create());

    [Fact]
    public async Task Invalid_transition_is_rejected() // BR-ST-01
    {
        using var context = TestDb.Create();
        var claim = context.AddClaim(policyId: Seeded.MeridianPolicyId, status: ClaimStatus.Draft);

        // Draft -> Closed is not an edge in the state machine.
        await Assert.ThrowsAsync<BusinessRuleException>(() => BuildHandler(context, new FakeCurrentUser("supervisor"))
            .Handle(new TransitionClaimStatusCommand(claim.ClaimId, ClaimStatus.Closed, null), CancellationToken.None));
    }

    [Fact]
    public async Task Open_transition_is_blocked_without_a_claimant() // BR-ST-02
    {
        using var context = TestDb.Create();
        var claim = context.AddClaim(policyId: Seeded.MeridianPolicyId, withClaimant: false, status: ClaimStatus.Draft);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => BuildHandler(context, new FakeCurrentUser("handler"))
            .Handle(new TransitionClaimStatusCommand(claim.ClaimId, ClaimStatus.Open, null), CancellationToken.None));
        Assert.Contains("ClaimParties", ex.Errors.Keys);
    }

    [Fact]
    public async Task Reopen_requires_supervisor_role() // BR-ST-04
    {
        using var context = TestDb.Create();
        var claim = context.AddClaim(policyId: Seeded.MeridianPolicyId, status: ClaimStatus.Closed);

        await Assert.ThrowsAsync<BusinessRuleException>(() => BuildHandler(context, new FakeCurrentUser("handler"))
            .Handle(new TransitionClaimStatusCommand(claim.ClaimId, ClaimStatus.Reopened, "found new evidence"), CancellationToken.None));
    }

    [Fact]
    public async Task Reopen_by_supervisor_cascades_immediately_to_Open() // FRS 4.2
    {
        using var context = TestDb.Create();
        var claim = context.AddClaim(policyId: Seeded.MeridianPolicyId, status: ClaimStatus.Closed);

        var result = await BuildHandler(context, new FakeCurrentUser("supervisor"))
            .Handle(new TransitionClaimStatusCommand(claim.ClaimId, ClaimStatus.Reopened, "found new evidence"), CancellationToken.None);

        Assert.Equal(ClaimStatus.Open, result.Status);
        Assert.Contains("CLAIM_REOPENED", _audit.Events);
    }

    [Fact]
    public async Task Closure_is_blocked_while_a_reserve_is_pending_approval() // CC-01
    {
        using var context = TestDb.Create();
        var claim = context.AddClaim(policyId: Seeded.MeridianPolicyId, status: ClaimStatus.Open);
        var submitter = new FakeCurrentUser("handler");
        await new ReserveService(context, submitter, _audit)
            .AddTransactionAsync(claim, ReserveComponentType.Indemnity, 50_000m, "reason", ReserveTransactionType.Add, CancellationToken.None);
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => BuildHandler(context, new FakeCurrentUser("supervisor"))
            .Handle(new TransitionClaimStatusCommand(claim.ClaimId, ClaimStatus.Closed, "closing"), CancellationToken.None));
        Assert.Contains("Reserves", ex.Errors.Keys);
    }
}

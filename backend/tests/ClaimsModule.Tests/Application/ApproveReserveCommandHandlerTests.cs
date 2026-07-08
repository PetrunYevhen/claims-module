using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Reserves.Commands.ApproveReserve;
using ClaimsModule.Application.Reserves.Common;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClaimsModule.Tests.Application;

public class ApproveReserveCommandHandlerTests
{
    private static readonly Guid SubmitterId = new("a1a1a1a1-0000-0000-0000-000000000001");
    private static readonly Guid SupervisorId = new("a1a1a1a1-0000-0000-0000-000000000002");

    private readonly RecordingAuditLog _audit = new();
    private readonly RecordingJobEnqueuer _enqueuer = new();

    /// <summary>Seeds a claim with a single pending reserve submitted by SubmitterId.</summary>
    private async Task<(Claim claim, ReserveHistory pending)> SeedPendingReserve(
        ClaimsModule.Persistence.ClaimsDbContext context, decimal amount)
    {
        var claim = context.AddClaim(policyId: Seeded.MeridianPolicyId);
        var submitter = new FakeCurrentUser("handler", SubmitterId);
        var pending = await new ReserveService(context, submitter, _audit)
            .AddTransactionAsync(claim, ReserveComponentType.Indemnity, amount, "reason", ReserveTransactionType.Add, CancellationToken.None);
        await context.SaveChangesAsync();
        return (claim, pending);
    }

    private ApproveReserveCommandHandler BuildHandler(ClaimsModule.Persistence.ClaimsDbContext context, FakeCurrentUser approver)
        => new(context, approver, _audit, _enqueuer, MapperFactory.Create());

    [Fact]
    public async Task Supervisor_approving_a_pending_reserve_applies_the_balance_and_enqueues_gl() // happy path
    {
        using var context = TestDb.Create();
        var (claim, pending) = await SeedPendingReserve(context, 50_000m);

        var approver = new FakeCurrentUser("supervisor", SupervisorId);
        var result = await BuildHandler(context, approver)
            .Handle(new ApproveReserveCommand(claim.ClaimId, pending.ReserveHistoryId), CancellationToken.None);

        Assert.Equal(ReserveApprovalStatus.Approved, result.ApprovalStatus);
        var component = await context.ClaimReserveComponents.SingleAsync(c => c.ClaimId == claim.ClaimId);
        Assert.Equal(50_000m, component.CurrentAmount);
        Assert.Single(_enqueuer.Enqueued);
        Assert.Contains("RESERVE_APPROVED", _audit.Events);
    }

    [Fact]
    public async Task Submitter_cannot_approve_their_own_reserve() // BR-R-03
    {
        using var context = TestDb.Create();
        var (claim, pending) = await SeedPendingReserve(context, 50_000m);

        // Same user id as the submitter, even though the role would otherwise permit it.
        var selfApprover = new FakeCurrentUser("supervisor", SubmitterId);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => BuildHandler(context, selfApprover)
            .Handle(new ApproveReserveCommand(claim.ClaimId, pending.ReserveHistoryId), CancellationToken.None));
        Assert.Contains("Self-approval", ex.Message);
        Assert.Empty(_enqueuer.Enqueued);
    }

    [Fact]
    public async Task Supervisor_cannot_approve_above_their_authority() // BR-R-02 (> 100k is manager only)
    {
        using var context = TestDb.Create();
        var (claim, pending) = await SeedPendingReserve(context, 150_000m);

        var approver = new FakeCurrentUser("supervisor", SupervisorId);

        await Assert.ThrowsAsync<BusinessRuleException>(() => BuildHandler(context, approver)
            .Handle(new ApproveReserveCommand(claim.ClaimId, pending.ReserveHistoryId), CancellationToken.None));
        Assert.Empty(_enqueuer.Enqueued);
    }
}

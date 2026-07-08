using ClaimsModule.Application.Claims.Commands.CreateClaim;
using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Services;
using ClaimsModule.Application.Reserves.Common;
using ClaimsModule.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClaimsModule.Tests.Application;

public class CreateClaimCommandHandlerTests
{
    private readonly FakeCurrentUser _user = new("handler");
    private readonly RecordingAuditLog _audit = new();
    private readonly RecordingJobEnqueuer _enqueuer = new();

    private CreateClaimCommandHandler BuildHandler(ClaimsModule.Persistence.ClaimsDbContext context)
    {
        var reserveService = new ReserveService(context, _user, _audit);
        return new CreateClaimCommandHandler(
            context, _user, new StubClaimNumberGenerator(), new ClaimValidationService(),
            reserveService, _enqueuer, _audit, MapperFactory.Create());
    }

    private static CreateClaimCommand Command(Guid? policyId, InitialReserveRequest? reserve = null) => new()
    {
        PolicyId = policyId,
        LossDate = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
        LossDescription = "A sufficiently long loss description for the intake.",
        CauseOfLossCode = Seeded.FireCode,
        Parties =
        [
            new CreatePartyRequest(PartyRole.Claimant, PartyType.Person, "Test", "Claimant", null, null, null)
        ],
        RiskObjects = [],
        InitialReserve = reserve
    };

    [Fact]
    public async Task Creates_claim_with_loss_event_and_party()
    {
        using var context = TestDb.Create();
        var result = await BuildHandler(context).Handle(Command(Seeded.MeridianPolicyId), CancellationToken.None);

        Assert.Equal("CLM-2026-0000001", result.ClaimNumber);
        Assert.Equal(ClaimStatus.Draft, result.Status);

        var persisted = await context.Claims.Include(c => c.LossEvent).Include(c => c.Parties)
            .SingleAsync(c => c.ClaimId == result.ClaimId);
        Assert.NotNull(persisted.LossEvent);
        Assert.Single(persisted.Parties);
        Assert.Contains("CLAIM_CREATED", _audit.Events);
    }

    [Fact]
    public async Task Initial_reserve_under_threshold_is_created_transactionally_and_auto_approved()
    {
        using var context = TestDb.Create();
        var command = Command(Seeded.MeridianPolicyId,
            new InitialReserveRequest(ReserveComponentType.Indemnity, 5_000m, "Initial estimate"));

        var result = await BuildHandler(context).Handle(command, CancellationToken.None);

        var component = await context.ClaimReserveComponents.Include(c => c.History)
            .SingleAsync(c => c.ClaimId == result.ClaimId);
        Assert.Equal(5_000m, component.CurrentAmount);
        Assert.Equal(ReserveApprovalStatus.AutoApproved, component.History.Single().ApprovalStatus);
        // GL posting is enqueued exactly once, only for the auto-approved path.
        Assert.Single(_enqueuer.Enqueued);
    }

    [Fact]
    public async Task Initial_reserve_over_threshold_is_pending_and_not_posted()
    {
        using var context = TestDb.Create();
        var command = Command(Seeded.MeridianPolicyId,
            new InitialReserveRequest(ReserveComponentType.Indemnity, 50_000m, "Large reserve"));

        var result = await BuildHandler(context).Handle(command, CancellationToken.None);

        var component = await context.ClaimReserveComponents.Include(c => c.History)
            .SingleAsync(c => c.ClaimId == result.ClaimId);
        Assert.Equal(0m, component.CurrentAmount); // balance only moves on approval
        Assert.Equal(ReserveApprovalStatus.PendingApproval, component.History.Single().ApprovalStatus);
        Assert.Empty(_enqueuer.Enqueued);
    }

    [Fact]
    public async Task Initial_reserve_without_a_policy_rolls_back_the_whole_create() // BR-C-06 + FRS 10.1 transaction
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = TestDb.Create(dbName))
        {
            var command = Command(policyId: null,
                new InitialReserveRequest(ReserveComponentType.Indemnity, 5_000m, "Should fail"));

            await Assert.ThrowsAsync<BusinessRuleException>(
                () => BuildHandler(context).Handle(command, CancellationToken.None));
        }

        // A fresh context on the same store proves nothing was persisted - no orphan claim.
        using var verify = TestDb.Create(dbName);
        Assert.Empty(await verify.Claims.ToListAsync());
        Assert.Empty(await verify.ReserveHistory.ToListAsync());
    }
}

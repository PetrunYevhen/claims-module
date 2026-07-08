using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using ClaimsModule.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Reserves.Common;

/// <summary>
/// Builds a reserve transaction (component + append-only history) against a claim, applying the
/// FRS 6.3 authority rules and writing the audit trail. Shared so a reserve is created identically
/// whether it originates from a standalone adjustment (CreateReserveCommand) or as the optional
/// initial reserve during FNOL intake (CreateClaimCommand) - in the latter case within the same
/// transaction as the claim itself, as FRS 5.2 / 10.1 require.
///
/// Attaches entities to the context and writes audit entries, but deliberately does NOT call
/// SaveChanges and does NOT enqueue the GL posting job: the caller owns the transaction commit and,
/// once committed, enqueues GL posting when the returned transaction came back auto-approved.
/// </summary>
public interface IReserveService
{
    Task<ReserveHistory> AddTransactionAsync(
        Claim claim,
        ReserveComponentType component,
        decimal amount,
        string changeReason,
        ReserveTransactionType transactionType,
        CancellationToken cancellationToken);
}

public class ReserveService(
    IClaimsDbContext context,
    ICurrentUserService currentUser,
    IAuditLogService auditLog) : IReserveService
{
    public async Task<ReserveHistory> AddTransactionAsync(
        Claim claim,
        ReserveComponentType component,
        decimal amount,
        string changeReason,
        ReserveTransactionType transactionType,
        CancellationToken cancellationToken)
    {
        // BR-C-06: reserve creation is blocked until a policy is linked.
        if (claim.PolicyId is null)
        {
            throw new BusinessRuleException("PolicyId",
                "No policy linked - claim requires policy association before financial actions are permitted.");
        }

        var reserveComponent = await context.ClaimReserveComponents
            .Include(c => c.History)
            .FirstOrDefaultAsync(c => c.ClaimId == claim.ClaimId && c.Component == component, cancellationToken);

        var now = DateTimeOffset.UtcNow;

        if (reserveComponent is null)
        {
            reserveComponent = new ClaimReserveComponent
            {
                ReserveComponentId = Guid.NewGuid(),
                ClaimId = claim.ClaimId,
                OrganisationId = currentUser.OrganisationId,
                Component = component,
                CurrentAmount = 0,
                Status = ReserveComponentStatus.Active,
                CreatedAt = now,
                UserCreated = currentUser.UserId
            };
            context.ClaimReserveComponents.Add(reserveComponent);
        }

        var previousBalance = reserveComponent.CurrentAmount;
        var newBalance = previousBalance + amount;
        var changeSequence = reserveComponent.History.Count + 1;
        var approvalStatus = ReserveAuthorityRules.DetermineInitialStatus(amount);
        var idempotencyKey = $"Reserve:{reserveComponent.ReserveComponentId}:Change:{changeSequence}";

        var history = new ReserveHistory
        {
            ReserveHistoryId = Guid.NewGuid(),
            ReserveComponentId = reserveComponent.ReserveComponentId,
            ClaimId = claim.ClaimId,
            OrganisationId = currentUser.OrganisationId,
            TransactionType = transactionType,
            Amount = amount,
            PreviousBalance = previousBalance,
            NewBalance = newBalance,
            ApprovalStatus = approvalStatus,
            ChangeReason = changeReason,
            PostingStatus = ReservePostingStatus.Pending,
            IdempotencyKey = idempotencyKey,
            ChangeSequence = changeSequence,
            SubmittedByUserId = currentUser.UserId,
            CreatedAt = now
        };

        if (approvalStatus == ReserveApprovalStatus.AutoApproved)
        {
            history.ApprovedByUserId = currentUser.UserId;
            history.ApprovedAt = now;
            reserveComponent.CurrentAmount = newBalance;
            reserveComponent.UpdatedAt = now;
            reserveComponent.UserModified = currentUser.UserId;
        }

        context.ReserveHistory.Add(history);

        await auditLog.LogAsync(claim.ClaimId, "RESERVE_CREATED",
            $"Reserve transaction submitted: {component} {amount:N2}.",
            relatedEntityId: history.ReserveHistoryId, relatedEntityType: nameof(ReserveHistory),
            cancellationToken: cancellationToken);

        if (approvalStatus == ReserveApprovalStatus.AutoApproved)
        {
            await auditLog.LogAsync(claim.ClaimId, "RESERVE_AUTO_APPROVED",
                $"Reserve transaction auto-approved: {component} {amount:N2} (<= authority threshold).",
                relatedEntityId: history.ReserveHistoryId, relatedEntityType: nameof(ReserveHistory),
                cancellationToken: cancellationToken);
        }

        // BR-R-05: aggregate cap is informational at submission time - it only blocks at approval.
        var approvedTotal = await context.ClaimReserveComponents
            .Where(c => c.ClaimId == claim.ClaimId)
            .SelectMany(c => c.History)
            .Where(h => h.ApprovalStatus == ReserveApprovalStatus.Approved || h.ApprovalStatus == ReserveApprovalStatus.AutoApproved)
            .SumAsync(h => h.Amount, cancellationToken);

        if (approvedTotal + amount > ReserveAuthorityRules.AggregateClaimLimit && !claim.AggregateLimitOverride)
        {
            await auditLog.LogAsync(claim.ClaimId, "VALIDATION_ISSUE_ADDED",
                "Total reserves will exceed $10,000,000. Manager override required before this can be approved.",
                relatedEntityId: history.ReserveHistoryId, relatedEntityType: nameof(ReserveHistory),
                cancellationToken: cancellationToken);
        }

        return history;
    }
}

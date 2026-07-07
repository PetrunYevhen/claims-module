using AutoMapper;
using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Application.Reserves.Dtos;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using ClaimsModule.Domain.Rules;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Reserves.Commands.CreateReserve;

public class CreateReserveCommandHandler(
    IClaimsDbContext context,
    ICurrentUserService currentUser,
    IAuditLogService auditLog,
    IBackgroundJobEnqueuer backgroundJobs,
    IMapper mapper) : IRequestHandler<CreateReserveCommand, ReserveHistoryDto>
{
    public async Task<ReserveHistoryDto> Handle(CreateReserveCommand request, CancellationToken cancellationToken)
    {
        var claim = await context.Claims
            .FirstOrDefaultAsync(c => c.ClaimId == request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), request.ClaimId);

        // BR-C-06: reserve creation is blocked until a policy is linked.
        if (claim.PolicyId is null)
        {
            throw new BusinessRuleException("PolicyId",
                "No policy linked - claim requires policy association before financial actions are permitted.");
        }

        var component = await context.ClaimReserveComponents
            .Include(c => c.History)
            .FirstOrDefaultAsync(c => c.ClaimId == request.ClaimId && c.Component == request.Component, cancellationToken);

        var now = DateTimeOffset.UtcNow;

        if (component is null)
        {
            component = new ClaimReserveComponent
            {
                ReserveComponentId = Guid.NewGuid(),
                ClaimId = request.ClaimId,
                OrganisationId = currentUser.OrganisationId,
                Component = request.Component,
                CurrentAmount = 0,
                Status = ReserveComponentStatus.Active,
                CreatedAt = now,
                UserCreated = currentUser.UserId
            };
            context.ClaimReserveComponents.Add(component);
        }

        var previousBalance = component.CurrentAmount;
        var newBalance = previousBalance + request.Amount;
        var changeSequence = component.History.Count + 1;
        var approvalStatus = ReserveAuthorityRules.DetermineInitialStatus(request.Amount);
        var idempotencyKey = $"Reserve:{component.ReserveComponentId}:Change:{changeSequence}";

        var history = new ReserveHistory
        {
            ReserveHistoryId = Guid.NewGuid(),
            ReserveComponentId = component.ReserveComponentId,
            ClaimId = request.ClaimId,
            OrganisationId = currentUser.OrganisationId,
            TransactionType = request.TransactionType,
            Amount = request.Amount,
            PreviousBalance = previousBalance,
            NewBalance = newBalance,
            ApprovalStatus = approvalStatus,
            ChangeReason = request.ChangeReason,
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
            component.CurrentAmount = newBalance;
            component.UpdatedAt = now;
            component.UserModified = currentUser.UserId;
        }

        context.ReserveHistory.Add(history);

        await auditLog.LogAsync(request.ClaimId, "RESERVE_CREATED",
            $"Reserve transaction submitted: {request.Component} {request.Amount:N2}.",
            relatedEntityId: history.ReserveHistoryId, relatedEntityType: nameof(ReserveHistory),
            cancellationToken: cancellationToken);

        if (approvalStatus == ReserveApprovalStatus.AutoApproved)
        {
            await auditLog.LogAsync(request.ClaimId, "RESERVE_AUTO_APPROVED",
                $"Reserve transaction auto-approved: {request.Component} {request.Amount:N2} (<= authority threshold).",
                relatedEntityId: history.ReserveHistoryId, relatedEntityType: nameof(ReserveHistory),
                cancellationToken: cancellationToken);
        }

        // BR-R-05: aggregate cap is informational at submission time - it only blocks at approval.
        var approvedTotal = await context.ClaimReserveComponents
            .Where(c => c.ClaimId == request.ClaimId)
            .SelectMany(c => c.History)
            .Where(h => h.ApprovalStatus == ReserveApprovalStatus.Approved || h.ApprovalStatus == ReserveApprovalStatus.AutoApproved)
            .SumAsync(h => h.Amount, cancellationToken);

        if (approvedTotal + request.Amount > ReserveAuthorityRules.AggregateClaimLimit && !claim.AggregateLimitOverride)
        {
            await auditLog.LogAsync(request.ClaimId, "VALIDATION_ISSUE_ADDED",
                "Total reserves will exceed $10,000,000. Manager override required before this can be approved.",
                relatedEntityId: history.ReserveHistoryId, relatedEntityType: nameof(ReserveHistory),
                cancellationToken: cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);

        if (approvalStatus == ReserveApprovalStatus.AutoApproved)
        {
            backgroundJobs.EnqueueGlPosting(history.ReserveHistoryId);
        }

        return mapper.Map<ReserveHistoryDto>(history);
    }
}

using AutoMapper;
using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Application.Reserves.Dtos;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using ClaimsModule.Domain.Rules;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Reserves.Commands.ApproveReserve;

public class ApproveReserveCommandHandler(
    IClaimsDbContext context,
    ICurrentUserService currentUser,
    IAuditLogService auditLog,
    IBackgroundJobEnqueuer backgroundJobs,
    IMapper mapper) : IRequestHandler<ApproveReserveCommand, ReserveHistoryDto>
{
    public async Task<ReserveHistoryDto> Handle(ApproveReserveCommand request, CancellationToken cancellationToken)
    {
        var history = await context.ReserveHistory
            .Include(h => h.ReserveComponent)
            .FirstOrDefaultAsync(h => h.ReserveHistoryId == request.ReserveHistoryId && h.ClaimId == request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(ReserveHistory), request.ReserveHistoryId);

        if (history.ApprovalStatus != ReserveApprovalStatus.PendingApproval)
        {
            throw new BusinessRuleException("ApprovalStatus",
                $"Reserve transaction is not pending approval (current status: {history.ApprovalStatus}).");
        }

        // BR-R-03: a user may not approve their own reserve transaction.
        if (history.SubmittedByUserId == currentUser.UserId)
        {
            throw new BusinessRuleException("Approval", "Self-approval is not permitted.");
        }

        if (!ReserveAuthorityRules.RoleCanApprove(currentUser.Role, history.Amount))
        {
            throw new BusinessRuleException("Role",
                "Your role does not have authority to approve this reserve amount.");
        }

        var claim = await context.Claims.FirstAsync(c => c.ClaimId == request.ClaimId, cancellationToken);

        // BR-R-05: aggregate cap enforced at approval time.
        var approvedTotal = await context.ClaimReserveComponents
            .Where(c => c.ClaimId == request.ClaimId)
            .SelectMany(c => c.History)
            .Where(h => h.ApprovalStatus == ReserveApprovalStatus.Approved || h.ApprovalStatus == ReserveApprovalStatus.AutoApproved)
            .SumAsync(h => h.Amount, cancellationToken);

        if (approvedTotal + history.Amount > ReserveAuthorityRules.AggregateClaimLimit && !claim.AggregateLimitOverride)
        {
            throw new BusinessRuleException("AggregateLimit",
                "Total reserves would exceed $10,000,000. A Manager must set the override flag before this can be approved.");
        }

        var now = DateTimeOffset.UtcNow;
        history.ApprovalStatus = ReserveApprovalStatus.Approved;
        history.ApprovedByUserId = currentUser.UserId;
        history.ApprovedAt = now;

        history.ReserveComponent.CurrentAmount = history.NewBalance;
        history.ReserveComponent.UpdatedAt = now;
        history.ReserveComponent.UserModified = currentUser.UserId;

        await auditLog.LogAsync(request.ClaimId, "RESERVE_APPROVED",
            $"Reserve transaction approved: {history.ReserveComponent.Component} {history.Amount:N2}.",
            relatedEntityId: history.ReserveHistoryId, relatedEntityType: nameof(ReserveHistory),
            cancellationToken: cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        backgroundJobs.EnqueueGlPosting(history.ReserveHistoryId);

        return mapper.Map<ReserveHistoryDto>(history);
    }
}

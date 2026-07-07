using AutoMapper;
using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Application.Reserves.Dtos;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using ClaimsModule.Domain.Rules;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Reserves.Commands.RejectReserve;

public class RejectReserveCommandHandler(
    IClaimsDbContext context,
    ICurrentUserService currentUser,
    IAuditLogService auditLog,
    IMapper mapper) : IRequestHandler<RejectReserveCommand, ReserveHistoryDto>
{
    public async Task<ReserveHistoryDto> Handle(RejectReserveCommand request, CancellationToken cancellationToken)
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

        if (!ReserveAuthorityRules.RoleCanApprove(currentUser.Role, history.Amount))
        {
            throw new BusinessRuleException("Role",
                "Your role does not have authority to reject this reserve amount.");
        }

        history.ApprovalStatus = ReserveApprovalStatus.Rejected;
        history.RejectedByUserId = currentUser.UserId;
        history.RejectedAt = DateTimeOffset.UtcNow;
        history.RejectionReason = request.RejectionReason;

        // BR-R-04: the original rejected transaction is retained as-is; the submitter creates a
        // new transaction (via CreateReserveCommand) to re-submit with a revised amount.
        await auditLog.LogAsync(request.ClaimId, "RESERVE_REJECTED",
            $"Reserve transaction rejected: {history.ReserveComponent.Component} {history.Amount:N2}.",
            oldValue: request.RejectionReason,
            relatedEntityId: history.ReserveHistoryId, relatedEntityType: nameof(ReserveHistory),
            cancellationToken: cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return mapper.Map<ReserveHistoryDto>(history);
    }
}

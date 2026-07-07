using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Reserves.Commands.RetractReserve;

public class RetractReserveCommandHandler(
    IClaimsDbContext context,
    ICurrentUserService currentUser,
    IAuditLogService auditLog) : IRequestHandler<RetractReserveCommand>
{
    public async Task Handle(RetractReserveCommand request, CancellationToken cancellationToken)
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

        // Only the original submitter may retract their own pending transaction (FRS 6.4).
        if (history.SubmittedByUserId != currentUser.UserId)
        {
            throw new BusinessRuleException("Retract", "Only the original submitter may retract a pending reserve.");
        }

        history.ApprovalStatus = ReserveApprovalStatus.Cancelled;

        await auditLog.LogAsync(request.ClaimId, "RESERVE_RETRACTED",
            $"Reserve transaction retracted: {history.ReserveComponent.Component} {history.Amount:N2}.",
            relatedEntityId: history.ReserveHistoryId, relatedEntityType: nameof(ReserveHistory),
            cancellationToken: cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }
}

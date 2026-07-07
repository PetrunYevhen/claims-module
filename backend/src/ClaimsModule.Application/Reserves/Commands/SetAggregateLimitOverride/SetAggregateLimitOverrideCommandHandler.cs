using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Reserves.Commands.SetAggregateLimitOverride;

/// <summary>
/// BR-R-05's override path has no dedicated REST endpoint in the FRS list (10.1-10.3) - without
/// one, a claim that exceeds the $10M aggregate cap could never be approved. This command is the
/// deliberate entry point for it: Manager-only, sets Claim.AggregateLimitOverride.
/// </summary>
public class SetAggregateLimitOverrideCommandHandler(
    IClaimsDbContext context,
    ICurrentUserService currentUser,
    IAuditLogService auditLog) : IRequestHandler<SetAggregateLimitOverrideCommand>
{
    public async Task Handle(SetAggregateLimitOverrideCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Role != "manager")
        {
            throw new BusinessRuleException("Role", "Only a Manager may set the aggregate limit override.");
        }

        var claim = await context.Claims.FirstOrDefaultAsync(c => c.ClaimId == request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), request.ClaimId);

        claim.AggregateLimitOverride = true;
        claim.UpdatedAt = DateTimeOffset.UtcNow;
        claim.UserModified = currentUser.UserId;

        await auditLog.LogAsync(request.ClaimId, "AGGREGATE_LIMIT_OVERRIDE_SET",
            $"Manager override enabled for the $10,000,000 aggregate reserve limit. Reason: {request.Reason}",
            newValue: request.Reason, cancellationToken: cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }
}

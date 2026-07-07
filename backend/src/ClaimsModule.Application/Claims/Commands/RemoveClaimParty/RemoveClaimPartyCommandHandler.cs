using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Claims.Commands.RemoveClaimParty;

public class RemoveClaimPartyCommandHandler(
    IClaimsDbContext context,
    ICurrentUserService currentUser,
    IAuditLogService auditLog) : IRequestHandler<RemoveClaimPartyCommand>
{
    public async Task Handle(RemoveClaimPartyCommand request, CancellationToken cancellationToken)
    {
        var claim = await context.Claims
            .Include(c => c.Parties)
            .FirstOrDefaultAsync(c => c.ClaimId == request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), request.ClaimId);

        var party = claim.Parties.FirstOrDefault(p => p.ClaimPartyId == request.PartyId)
            ?? throw new NotFoundException(nameof(ClaimParty), request.PartyId);

        if (!party.IsActive)
        {
            return;
        }

        // BR-P-01: at least one active Claimant must remain.
        var isLastActiveClaimant = party.PartyRole == PartyRole.Claimant
            && claim.Parties.Count(p => p.PartyRole == PartyRole.Claimant && p.IsActive) == 1;

        if (isLastActiveClaimant)
        {
            throw new BusinessRuleException("ClaimParties",
                "Cannot remove the last active Claimant party on a claim.");
        }

        party.IsActive = false;
        party.UpdatedAt = DateTimeOffset.UtcNow;
        party.UserModified = currentUser.UserId;

        await auditLog.LogAsync(claim.ClaimId, "PARTY_REMOVED",
            $"Party removed: {party.PartyRole} ({party.PartyType}).",
            relatedEntityId: party.ClaimPartyId, relatedEntityType: nameof(ClaimParty),
            cancellationToken: cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }
}

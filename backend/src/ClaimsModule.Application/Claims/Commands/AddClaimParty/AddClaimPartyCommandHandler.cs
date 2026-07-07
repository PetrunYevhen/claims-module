using AutoMapper;
using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Claims.Commands.AddClaimParty;

public class AddClaimPartyCommandHandler(
    IClaimsDbContext context,
    ICurrentUserService currentUser,
    IAuditLogService auditLog,
    IMapper mapper) : IRequestHandler<AddClaimPartyCommand, ClaimPartyDto>
{
    public async Task<ClaimPartyDto> Handle(AddClaimPartyCommand request, CancellationToken cancellationToken)
    {
        var claimExists = await context.Claims.AnyAsync(c => c.ClaimId == request.ClaimId, cancellationToken);
        if (!claimExists)
        {
            throw new NotFoundException(nameof(Claim), request.ClaimId);
        }

        var now = DateTimeOffset.UtcNow;
        var party = new ClaimParty
        {
            ClaimPartyId = Guid.NewGuid(),
            ClaimId = request.ClaimId,
            OrganisationId = currentUser.OrganisationId,
            PartyRole = request.PartyRole,
            PartyType = request.PartyType,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CompanyName = request.CompanyName,
            Email = request.Email,
            Phone = request.Phone,
            IsActive = true,
            CreatedAt = now,
            UserCreated = currentUser.UserId
        };

        context.ClaimParties.Add(party);

        await auditLog.LogAsync(request.ClaimId, "PARTY_ADDED",
            $"Party added: {request.PartyRole} ({request.PartyType}).",
            relatedEntityId: party.ClaimPartyId, relatedEntityType: nameof(ClaimParty),
            cancellationToken: cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return mapper.Map<ClaimPartyDto>(party);
    }
}

using MediatR;

namespace ClaimsModule.Application.Claims.Commands.RemoveClaimParty;

public record RemoveClaimPartyCommand(Guid ClaimId, Guid PartyId) : IRequest;

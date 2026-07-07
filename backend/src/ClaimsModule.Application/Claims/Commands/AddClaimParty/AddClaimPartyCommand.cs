using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Domain.Enums;
using MediatR;

namespace ClaimsModule.Application.Claims.Commands.AddClaimParty;

public record AddClaimPartyCommand(
    Guid ClaimId,
    PartyRole PartyRole,
    PartyType PartyType,
    string? FirstName,
    string? LastName,
    string? CompanyName,
    string? Email,
    string? Phone) : IRequest<ClaimPartyDto>;

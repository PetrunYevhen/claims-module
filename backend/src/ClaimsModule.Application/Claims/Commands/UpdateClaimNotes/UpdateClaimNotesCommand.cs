using ClaimsModule.Application.Claims.Dtos;
using MediatR;

namespace ClaimsModule.Application.Claims.Commands.UpdateClaimNotes;

public record UpdateClaimNotesCommand(Guid ClaimId, string? Notes) : IRequest<ClaimDetailDto>;

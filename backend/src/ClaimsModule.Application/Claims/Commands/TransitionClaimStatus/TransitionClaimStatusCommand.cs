using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Domain.Enums;
using MediatR;

namespace ClaimsModule.Application.Claims.Commands.TransitionClaimStatus;

public record TransitionClaimStatusCommand(Guid ClaimId, ClaimStatus TargetStatus, string? Reason)
    : IRequest<ClaimDetailDto>;

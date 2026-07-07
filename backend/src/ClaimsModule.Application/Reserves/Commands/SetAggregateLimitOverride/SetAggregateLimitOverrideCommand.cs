using MediatR;

namespace ClaimsModule.Application.Reserves.Commands.SetAggregateLimitOverride;

public record SetAggregateLimitOverrideCommand(Guid ClaimId, string Reason) : IRequest;

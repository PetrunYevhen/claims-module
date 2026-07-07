using MediatR;

namespace ClaimsModule.Application.Reserves.Commands.RetractReserve;

public record RetractReserveCommand(Guid ClaimId, Guid ReserveHistoryId) : IRequest;

using ClaimsModule.Application.Reserves.Dtos;
using MediatR;

namespace ClaimsModule.Application.Reserves.Commands.RejectReserve;

public record RejectReserveCommand(Guid ClaimId, Guid ReserveHistoryId, string RejectionReason)
    : IRequest<ReserveHistoryDto>;

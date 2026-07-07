using ClaimsModule.Application.Reserves.Dtos;
using MediatR;

namespace ClaimsModule.Application.Reserves.Commands.ApproveReserve;

public record ApproveReserveCommand(Guid ClaimId, Guid ReserveHistoryId) : IRequest<ReserveHistoryDto>;

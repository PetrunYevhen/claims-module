using ClaimsModule.Application.Reserves.Dtos;
using MediatR;

namespace ClaimsModule.Application.Reserves.Queries.GetClaimReserves;

public record GetClaimReservesQuery(Guid ClaimId) : IRequest<ReserveSummaryDto>;

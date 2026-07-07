using ClaimsModule.Application.Reserves.Dtos;
using ClaimsModule.Domain.Enums;
using MediatR;

namespace ClaimsModule.Application.Reserves.Commands.CreateReserve;

public record CreateReserveCommand(
    Guid ClaimId,
    ReserveComponentType Component,
    decimal Amount,
    string ChangeReason,
    ReserveTransactionType TransactionType) : IRequest<ReserveHistoryDto>;

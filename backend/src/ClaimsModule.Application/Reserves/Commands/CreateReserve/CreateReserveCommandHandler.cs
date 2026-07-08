using AutoMapper;
using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Application.Reserves.Common;
using ClaimsModule.Application.Reserves.Dtos;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Reserves.Commands.CreateReserve;

public class CreateReserveCommandHandler(
    IClaimsDbContext context,
    IReserveService reserveService,
    IBackgroundJobEnqueuer backgroundJobs,
    IMapper mapper) : IRequestHandler<CreateReserveCommand, ReserveHistoryDto>
{
    public async Task<ReserveHistoryDto> Handle(CreateReserveCommand request, CancellationToken cancellationToken)
    {
        var claim = await context.Claims
            .FirstOrDefaultAsync(c => c.ClaimId == request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), request.ClaimId);

        var history = await reserveService.AddTransactionAsync(
            claim, request.Component, request.Amount, request.ChangeReason, request.TransactionType, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        // GL posting is only enqueued once the change is approved - here, only the auto-approved path.
        if (history.ApprovalStatus == ReserveApprovalStatus.AutoApproved)
        {
            backgroundJobs.EnqueueGlPosting(history.ReserveHistoryId);
        }

        return mapper.Map<ReserveHistoryDto>(history);
    }
}

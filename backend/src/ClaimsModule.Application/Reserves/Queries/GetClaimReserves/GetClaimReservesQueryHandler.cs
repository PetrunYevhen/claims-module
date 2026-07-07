using AutoMapper;
using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Application.Reserves.Dtos;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Reserves.Queries.GetClaimReserves;

public class GetClaimReservesQueryHandler(IClaimsDbContext context, IMapper mapper)
    : IRequestHandler<GetClaimReservesQuery, ReserveSummaryDto>
{
    public async Task<ReserveSummaryDto> Handle(GetClaimReservesQuery request, CancellationToken cancellationToken)
    {
        var claim = await context.Claims
            .FirstOrDefaultAsync(c => c.ClaimId == request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), request.ClaimId);

        var components = await context.ClaimReserveComponents
            .Include(c => c.History)
            .Where(c => c.ClaimId == request.ClaimId)
            .ToListAsync(cancellationToken);

        var history = components
            .SelectMany(c => c.History)
            .OrderByDescending(h => h.CreatedAt)
            .ToList();

        return new ReserveSummaryDto
        {
            Components = mapper.Map<List<ReserveComponentDto>>(components),
            History = mapper.Map<List<ReserveHistoryDto>>(history),
            TotalApproved = history
                .Where(h => h.ApprovalStatus is ReserveApprovalStatus.Approved or ReserveApprovalStatus.AutoApproved)
                .Sum(h => h.Amount),
            AggregateLimitOverride = claim.AggregateLimitOverride
        };
    }
}

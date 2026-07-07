using AutoMapper;
using ClaimsModule.Application.Claims.Common;
using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Claims.Queries.GetClaimDetail;

public class GetClaimDetailQueryHandler(
    IClaimsDbContext context,
    IClaimValidationService validationService,
    IMapper mapper) : IRequestHandler<GetClaimDetailQuery, ClaimDetailDto>
{
    public async Task<ClaimDetailDto> Handle(GetClaimDetailQuery request, CancellationToken cancellationToken)
    {
        var claim = await context.Claims
            .Include(c => c.LossEvent)
            .Include(c => c.Parties)
            .Include(c => c.RiskObjects)
            .Include(c => c.ReserveComponents).ThenInclude(rc => rc.History)
            .FirstOrDefaultAsync(c => c.ClaimId == request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), request.ClaimId);

        var policy = claim.PolicyId.HasValue
            ? await context.Policies.FirstOrDefaultAsync(p => p.PolicyId == claim.PolicyId, cancellationToken)
            : null;

        var causeOfLossCode = claim.LossEvent is null
            ? null
            : await context.CauseOfLossCodes.FirstOrDefaultAsync(c => c.Code == claim.LossEvent.CauseOfLossCode, cancellationToken);

        var issues = validationService.Evaluate(claim, claim.LossEvent, policy, causeOfLossCode, claim.Parties);

        return ClaimDetailDtoFactory.Build(mapper, claim, issues);
    }
}

using ClaimsModule.Application.Reference.Dtos;
using ClaimsModule.Domain.Enums;
using ClaimsModule.Domain.Rules;
using MediatR;

namespace ClaimsModule.Application.Reference.Queries.GetClaimStatuses;

/// <summary>Pure domain lookup - no DB access needed, the state machine is the source of truth.</summary>
public class GetClaimStatusesQueryHandler : IRequestHandler<GetClaimStatusesQuery, List<ClaimStatusDto>>
{
    public Task<List<ClaimStatusDto>> Handle(GetClaimStatusesQuery request, CancellationToken cancellationToken)
    {
        var result = Enum.GetValues<ClaimStatus>()
            .Select(status => new ClaimStatusDto(
                status.ToString(),
                ClaimStatusTransitionRules.GetValidNextStatuses(status).Select(s => s.ToString()).ToList()))
            .ToList();

        return Task.FromResult(result);
    }
}

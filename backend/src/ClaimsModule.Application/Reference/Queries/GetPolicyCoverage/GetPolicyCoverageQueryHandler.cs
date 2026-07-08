using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Application.Reference.Dtos;
using ClaimsModule.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Reference.Queries.GetPolicyCoverage;

public class GetPolicyCoverageQueryHandler(IClaimsDbContext context)
    : IRequestHandler<GetPolicyCoverageQuery, PolicyCoverageDto>
{
    public async Task<PolicyCoverageDto> Handle(GetPolicyCoverageQuery request, CancellationToken cancellationToken)
    {
        var policy = await context.Policies
            .FirstOrDefaultAsync(p => p.PolicyId == request.PolicyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Policy), request.PolicyId);

        // CoverageTypes is stored as a comma-separated list (FRS 9.10) - split into a clean array.
        var coverageTypes = policy.CoverageTypes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        return new PolicyCoverageDto(policy.PolicyId, policy.PolicyNumber, coverageTypes);
    }
}

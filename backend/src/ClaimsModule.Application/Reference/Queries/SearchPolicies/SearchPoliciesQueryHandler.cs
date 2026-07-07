using AutoMapper;
using AutoMapper.QueryableExtensions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Application.Reference.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Reference.Queries.SearchPolicies;

public class SearchPoliciesQueryHandler(IClaimsDbContext context, IConfigurationProvider mapperConfig)
    : IRequestHandler<SearchPoliciesQuery, List<PolicyDto>>
{
    public async Task<List<PolicyDto>> Handle(SearchPoliciesQuery request, CancellationToken cancellationToken)
    {
        var query = context.Policies.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            query = query.Where(p =>
                p.PolicyNumber.Contains(request.Query) ||
                p.ClientName.Contains(request.Query));
        }

        return await query
            .OrderBy(p => p.PolicyNumber)
            .ProjectTo<PolicyDto>(mapperConfig)
            .ToListAsync(cancellationToken);
    }
}

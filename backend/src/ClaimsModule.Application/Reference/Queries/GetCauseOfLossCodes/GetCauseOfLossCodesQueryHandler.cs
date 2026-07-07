using AutoMapper;
using AutoMapper.QueryableExtensions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Application.Reference.Dtos;
using ClaimsModule.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Reference.Queries.GetCauseOfLossCodes;

public class GetCauseOfLossCodesQueryHandler(IClaimsDbContext context, IConfigurationProvider mapperConfig)
    : IRequestHandler<GetCauseOfLossCodesQuery, List<CauseOfLossCodeDto>>
{
    public async Task<List<CauseOfLossCodeDto>> Handle(GetCauseOfLossCodesQuery request, CancellationToken cancellationToken)
    {
        var query = context.CauseOfLossCodes.Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(request.PerilCategory)
            && Enum.TryParse<PerilCategory>(request.PerilCategory, true, out var category))
        {
            query = query.Where(c => c.PerilCategory == category);
        }

        return await query
            .OrderBy(c => c.SortOrder)
            .ProjectTo<CauseOfLossCodeDto>(mapperConfig)
            .ToListAsync(cancellationToken);
    }
}

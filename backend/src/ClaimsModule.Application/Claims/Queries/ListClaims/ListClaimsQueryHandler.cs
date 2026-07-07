using AutoMapper;
using AutoMapper.QueryableExtensions;
using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Application.Common.Models;
using ClaimsModule.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Claims.Queries.ListClaims;

public class ListClaimsQueryHandler(IClaimsDbContext context, IConfigurationProvider mapperConfig)
    : IRequestHandler<ListClaimsQuery, PagedResult<ClaimSummaryDto>>
{
    public async Task<PagedResult<ClaimSummaryDto>> Handle(ListClaimsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Claim> query = context.Claims;

        if (request.Statuses is { Length: > 0 })
        {
            query = query.Where(c => request.Statuses.Contains(c.Status));
        }

        if (request.DateFrom.HasValue)
        {
            query = query.Where(c => c.LossEvent != null && c.LossEvent.LossDate >= request.DateFrom);
        }

        if (request.DateTo.HasValue)
        {
            query = query.Where(c => c.LossEvent != null && c.LossEvent.LossDate <= request.DateTo);
        }

        if (request.AssignedHandlerId.HasValue)
        {
            query = query.Where(c => c.AssignedHandlerId == request.AssignedHandlerId);
        }

        if (!string.IsNullOrWhiteSpace(request.CauseOfLossCode))
        {
            query = query.Where(c => c.LossEvent != null && c.LossEvent.CauseOfLossCode == request.CauseOfLossCode);
        }

        if (request.PolicyId.HasValue)
        {
            query = query.Where(c => c.PolicyId == request.PolicyId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(c =>
                c.ClaimNumber.Contains(request.Search) ||
                (c.ClientName != null && c.ClientName.Contains(request.Search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.ReportedDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectTo<ClaimSummaryDto>(mapperConfig)
            .ToListAsync(cancellationToken);

        return new PagedResult<ClaimSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}

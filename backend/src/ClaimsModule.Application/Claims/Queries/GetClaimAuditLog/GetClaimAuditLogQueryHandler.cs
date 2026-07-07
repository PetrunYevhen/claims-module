using AutoMapper;
using AutoMapper.QueryableExtensions;
using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Claims.Queries.GetClaimAuditLog;

public class GetClaimAuditLogQueryHandler(IClaimsDbContext context, IConfigurationProvider mapperConfig)
    : IRequestHandler<GetClaimAuditLogQuery, PagedResult<AuditLogEntryDto>>
{
    public async Task<PagedResult<AuditLogEntryDto>> Handle(GetClaimAuditLogQuery request, CancellationToken cancellationToken)
    {
        var query = context.ClaimAuditLog
            .Where(a => a.ClaimId == request.ClaimId)
            .OrderByDescending(a => a.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectTo<AuditLogEntryDto>(mapperConfig)
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogEntryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}

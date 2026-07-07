using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Application.Common.Models;
using MediatR;

namespace ClaimsModule.Application.Claims.Queries.GetClaimAuditLog;

public record GetClaimAuditLogQuery(Guid ClaimId, int Page = 1, int PageSize = 50)
    : IRequest<PagedResult<AuditLogEntryDto>>;

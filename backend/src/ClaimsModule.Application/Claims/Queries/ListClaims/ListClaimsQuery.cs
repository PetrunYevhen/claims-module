using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Application.Common.Models;
using ClaimsModule.Domain.Enums;
using MediatR;

namespace ClaimsModule.Application.Claims.Queries.ListClaims;

public record ListClaimsQuery : IRequest<PagedResult<ClaimSummaryDto>>
{
    public ClaimStatus[]? Statuses { get; init; }
    public DateTimeOffset? DateFrom { get; init; }
    public DateTimeOffset? DateTo { get; init; }
    public Guid? AssignedHandlerId { get; init; }
    public string? CauseOfLossCode { get; init; }
    public Guid? PolicyId { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

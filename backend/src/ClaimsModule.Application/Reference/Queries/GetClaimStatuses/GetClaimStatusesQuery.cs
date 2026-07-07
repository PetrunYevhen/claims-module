using ClaimsModule.Application.Reference.Dtos;
using MediatR;

namespace ClaimsModule.Application.Reference.Queries.GetClaimStatuses;

public record GetClaimStatusesQuery : IRequest<List<ClaimStatusDto>>;

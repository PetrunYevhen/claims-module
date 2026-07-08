using ClaimsModule.Application.Reference.Dtos;
using MediatR;

namespace ClaimsModule.Application.Reference.Queries.GetPolicyCoverage;

public record GetPolicyCoverageQuery(Guid PolicyId) : IRequest<PolicyCoverageDto>;

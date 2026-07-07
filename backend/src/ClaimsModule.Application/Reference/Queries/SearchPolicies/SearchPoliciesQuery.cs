using ClaimsModule.Application.Reference.Dtos;
using MediatR;

namespace ClaimsModule.Application.Reference.Queries.SearchPolicies;

public record SearchPoliciesQuery(string? Query) : IRequest<List<PolicyDto>>;

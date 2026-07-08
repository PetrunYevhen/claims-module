using ClaimsModule.Application.Reference.Dtos;
using ClaimsModule.Application.Reference.Queries.GetPolicyCoverage;
using ClaimsModule.Application.Reference.Queries.SearchPolicies;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsModule.API.Controllers;

[ApiController]
[Route("api/policies")]
public class PoliciesController(IMediator mediator) : ControllerBase
{
    [HttpGet("search")]
    public async Task<ActionResult<List<PolicyDto>>> Search(
        [FromQuery] string? q, CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new SearchPoliciesQuery(q), cancellationToken));
    }

    [HttpGet("{id:guid}/coverage")]
    public async Task<ActionResult<PolicyCoverageDto>> GetCoverage(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new GetPolicyCoverageQuery(id), cancellationToken));
    }
}

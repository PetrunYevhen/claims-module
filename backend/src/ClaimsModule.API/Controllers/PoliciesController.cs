using ClaimsModule.Application.Reference.Dtos;
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
}

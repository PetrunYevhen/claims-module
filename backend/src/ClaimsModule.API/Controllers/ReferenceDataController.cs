using ClaimsModule.Application.Reference.Dtos;
using ClaimsModule.Application.Reference.Queries.GetCauseOfLossCodes;
using ClaimsModule.Application.Reference.Queries.GetClaimStatuses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsModule.API.Controllers;

[ApiController]
[Route("api/reference")]
public class ReferenceDataController(IMediator mediator) : ControllerBase
{
    [HttpGet("cause-of-loss-codes")]
    public async Task<ActionResult<List<CauseOfLossCodeDto>>> GetCauseOfLossCodes(
        [FromQuery] string? perilCategory, CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new GetCauseOfLossCodesQuery(perilCategory), cancellationToken));
    }

    [HttpGet("claim-statuses")]
    public async Task<ActionResult<List<ClaimStatusDto>>> GetClaimStatuses(CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new GetClaimStatusesQuery(), cancellationToken));
    }
}

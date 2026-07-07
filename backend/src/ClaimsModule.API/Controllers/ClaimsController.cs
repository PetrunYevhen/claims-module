using ClaimsModule.Application.Claims.Commands.AddClaimParty;
using ClaimsModule.Application.Claims.Commands.CreateClaim;
using ClaimsModule.Application.Claims.Commands.RemoveClaimParty;
using ClaimsModule.Application.Claims.Commands.TransitionClaimStatus;
using ClaimsModule.Application.Claims.Commands.UpdateClaimNotes;
using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Application.Claims.Queries.GetClaimAuditLog;
using ClaimsModule.Application.Claims.Queries.GetClaimDetail;
using ClaimsModule.Application.Claims.Queries.ListClaims;
using ClaimsModule.Application.Common.Models;
using ClaimsModule.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsModule.API.Controllers;

[ApiController]
[Route("api/claims")]
public class ClaimsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ClaimDetailDto>> Create(CreateClaimCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.ClaimId }, result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ClaimSummaryDto>>> List(
        [FromQuery] ListClaimsQuery query, CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClaimDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new GetClaimDetailQuery(id), cancellationToken));
    }

    [HttpGet("{id:guid}/audit")]
    public async Task<ActionResult<PagedResult<AuditLogEntryDto>>> GetAuditLog(
        Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        return Ok(await mediator.Send(new GetClaimAuditLogQuery(id, page, pageSize), cancellationToken));
    }

    public record TransitionStatusRequest(ClaimStatus TargetStatus, string? Reason);

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<ClaimDetailDto>> TransitionStatus(
        Guid id, TransitionStatusRequest request, CancellationToken cancellationToken)
    {
        var command = new TransitionClaimStatusCommand(id, request.TargetStatus, request.Reason);
        return Ok(await mediator.Send(command, cancellationToken));
    }

    public record AddPartyRequest(
        PartyRole PartyRole,
        PartyType PartyType,
        string? FirstName,
        string? LastName,
        string? CompanyName,
        string? Email,
        string? Phone);

    [HttpPost("{id:guid}/parties")]
    public async Task<ActionResult<ClaimPartyDto>> AddParty(
        Guid id, AddPartyRequest request, CancellationToken cancellationToken)
    {
        var command = new AddClaimPartyCommand(
            id, request.PartyRole, request.PartyType,
            request.FirstName, request.LastName, request.CompanyName, request.Email, request.Phone);
        return Ok(await mediator.Send(command, cancellationToken));
    }

    [HttpDelete("{id:guid}/parties/{partyId:guid}")]
    public async Task<IActionResult> RemoveParty(Guid id, Guid partyId, CancellationToken cancellationToken)
    {
        await mediator.Send(new RemoveClaimPartyCommand(id, partyId), cancellationToken);
        return NoContent();
    }

    public record UpdateNotesRequest(string? Notes);

    [HttpPut("{id:guid}/notes")]
    public async Task<ActionResult<ClaimDetailDto>> UpdateNotes(
        Guid id, UpdateNotesRequest request, CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new UpdateClaimNotesCommand(id, request.Notes), cancellationToken));
    }
}

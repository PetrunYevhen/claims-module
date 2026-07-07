using ClaimsModule.Application.Reserves.Commands.ApproveReserve;
using ClaimsModule.Application.Reserves.Commands.CreateReserve;
using ClaimsModule.Application.Reserves.Commands.RejectReserve;
using ClaimsModule.Application.Reserves.Commands.RetractReserve;
using ClaimsModule.Application.Reserves.Commands.SetAggregateLimitOverride;
using ClaimsModule.Application.Reserves.Dtos;
using ClaimsModule.Application.Reserves.Queries.GetClaimReserves;
using ClaimsModule.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsModule.API.Controllers;

[ApiController]
[Route("api/claims/{claimId:guid}/reserves")]
public class ReservesController(IMediator mediator) : ControllerBase
{
    public record CreateReserveRequest(
        ReserveComponentType Component,
        decimal Amount,
        string ChangeReason,
        ReserveTransactionType TransactionType);

    [HttpPost]
    public async Task<ActionResult<ReserveHistoryDto>> Create(
        Guid claimId, CreateReserveRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateReserveCommand(
            claimId, request.Component, request.Amount, request.ChangeReason, request.TransactionType);
        return Ok(await mediator.Send(command, cancellationToken));
    }

    [HttpGet]
    public async Task<ActionResult<ReserveSummaryDto>> Get(Guid claimId, CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new GetClaimReservesQuery(claimId), cancellationToken));
    }

    [HttpPost("{reserveHistoryId:guid}/approve")]
    public async Task<ActionResult<ReserveHistoryDto>> Approve(
        Guid claimId, Guid reserveHistoryId, CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new ApproveReserveCommand(claimId, reserveHistoryId), cancellationToken));
    }

    public record RejectReserveRequest(string RejectionReason);

    [HttpPost("{reserveHistoryId:guid}/reject")]
    public async Task<ActionResult<ReserveHistoryDto>> Reject(
        Guid claimId, Guid reserveHistoryId, RejectReserveRequest request, CancellationToken cancellationToken)
    {
        var command = new RejectReserveCommand(claimId, reserveHistoryId, request.RejectionReason);
        return Ok(await mediator.Send(command, cancellationToken));
    }

    [HttpPost("{reserveHistoryId:guid}/retract")]
    public async Task<IActionResult> Retract(
        Guid claimId, Guid reserveHistoryId, CancellationToken cancellationToken)
    {
        await mediator.Send(new RetractReserveCommand(claimId, reserveHistoryId), cancellationToken);
        return NoContent();
    }

    public record OverrideLimitRequest(string Reason);

    [HttpPost("override-limit")]
    public async Task<IActionResult> OverrideAggregateLimit(
        Guid claimId, OverrideLimitRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new SetAggregateLimitOverrideCommand(claimId, request.Reason), cancellationToken);
        return NoContent();
    }
}

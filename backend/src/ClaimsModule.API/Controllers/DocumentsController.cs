using ClaimsModule.Application.Claims.Commands.UploadDocument;
using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Application.Claims.Queries.GetClaimDocuments;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsModule.API.Controllers;

[ApiController]
[Route("api/claims/{claimId:guid}/documents")]
public class DocumentsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<ActionResult<ClaimDocumentDto>> Upload(
        Guid claimId,
        IFormFile file,
        [FromForm] string documentType,
        [FromForm] string? notes,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var command = new UploadDocumentCommand(
            claimId, file.FileName, file.ContentType, file.Length, stream, documentType, notes);
        return Ok(await mediator.Send(command, cancellationToken));
    }

    [HttpGet]
    public async Task<ActionResult<List<ClaimDocumentDto>>> List(Guid claimId, CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new GetClaimDocumentsQuery(claimId), cancellationToken));
    }
}

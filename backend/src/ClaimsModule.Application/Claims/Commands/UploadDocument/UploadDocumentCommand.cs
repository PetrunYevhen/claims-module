using ClaimsModule.Application.Claims.Dtos;
using MediatR;

namespace ClaimsModule.Application.Claims.Commands.UploadDocument;

public record UploadDocumentCommand(
    Guid ClaimId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    Stream Content,
    string DocumentType,
    string? Notes) : IRequest<ClaimDocumentDto>;

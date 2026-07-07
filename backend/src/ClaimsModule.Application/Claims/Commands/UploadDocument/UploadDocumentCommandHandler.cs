using AutoMapper;
using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Claims.Commands.UploadDocument;

public class UploadDocumentCommandHandler(
    IClaimsDbContext context,
    ICurrentUserService currentUser,
    IStorageService storage,
    IAuditLogService auditLog,
    IMapper mapper) : IRequestHandler<UploadDocumentCommand, ClaimDocumentDto>
{
    public async Task<ClaimDocumentDto> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        var claimExists = await context.Claims.AnyAsync(c => c.ClaimId == request.ClaimId, cancellationToken);
        if (!claimExists)
        {
            throw new NotFoundException(nameof(Claim), request.ClaimId);
        }

        var sanitizedFileName = FileNameSanitizer.Sanitize(request.FileName);
        // BR-D-01: claim-documents/{organisationId}/{claimId}/{filename} - "claim-documents" is the
        // Azure container name (configured separately); this path is relative to that container / root.
        var blobPath = $"{currentUser.OrganisationId}/{request.ClaimId}/{sanitizedFileName}";

        await storage.UploadAsync(blobPath, request.Content, request.ContentType, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var document = new ClaimDocument
        {
            ClaimDocumentId = Guid.NewGuid(),
            ClaimId = request.ClaimId,
            OrganisationId = currentUser.OrganisationId,
            DocumentType = request.DocumentType,
            DocumentName = sanitizedFileName,
            BlobPath = blobPath,
            ContentType = request.ContentType,
            FileSizeBytes = request.FileSizeBytes,
            UploadedAt = now,
            UploadedByUserId = currentUser.UserId,
            Notes = request.Notes,
            CreatedAt = now,
            UserCreated = currentUser.UserId
        };

        context.ClaimDocuments.Add(document);

        await auditLog.LogAsync(request.ClaimId, "DOCUMENT_UPLOADED",
            $"Document uploaded: {sanitizedFileName}.",
            relatedEntityId: document.ClaimDocumentId, relatedEntityType: nameof(ClaimDocument),
            cancellationToken: cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        var dto = mapper.Map<ClaimDocumentDto>(document);
        dto.DownloadUrl = await storage.GetDownloadUrlAsync(blobPath, TimeSpan.FromHours(1), cancellationToken);
        return dto;
    }
}

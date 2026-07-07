using AutoMapper;
using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Claims.Queries.GetClaimDocuments;

public class GetClaimDocumentsQueryHandler(IClaimsDbContext context, IStorageService storage, IMapper mapper)
    : IRequestHandler<GetClaimDocumentsQuery, List<ClaimDocumentDto>>
{
    public async Task<List<ClaimDocumentDto>> Handle(GetClaimDocumentsQuery request, CancellationToken cancellationToken)
    {
        var documents = await context.ClaimDocuments
            .Where(d => d.ClaimId == request.ClaimId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(cancellationToken);

        var dtos = new List<ClaimDocumentDto>(documents.Count);
        foreach (var document in documents)
        {
            var dto = mapper.Map<ClaimDocumentDto>(document);
            dto.DownloadUrl = await storage.GetDownloadUrlAsync(document.BlobPath, TimeSpan.FromHours(1), cancellationToken);
            dtos.Add(dto);
        }

        return dtos;
    }
}

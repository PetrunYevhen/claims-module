using ClaimsModule.Application.Claims.Dtos;
using MediatR;

namespace ClaimsModule.Application.Claims.Queries.GetClaimDocuments;

public record GetClaimDocumentsQuery(Guid ClaimId) : IRequest<List<ClaimDocumentDto>>;

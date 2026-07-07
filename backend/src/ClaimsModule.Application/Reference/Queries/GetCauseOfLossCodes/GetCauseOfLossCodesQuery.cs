using ClaimsModule.Application.Reference.Dtos;
using MediatR;

namespace ClaimsModule.Application.Reference.Queries.GetCauseOfLossCodes;

public record GetCauseOfLossCodesQuery(string? PerilCategory) : IRequest<List<CauseOfLossCodeDto>>;

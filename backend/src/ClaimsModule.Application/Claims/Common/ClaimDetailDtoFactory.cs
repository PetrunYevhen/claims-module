using AutoMapper;
using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Rules;

namespace ClaimsModule.Application.Claims.Common;

/// <summary>
/// Shared DTO assembly for handlers that return a full ClaimDetailDto (transition, get-detail).
/// Requires claim.LossEvent / claim.Parties / claim.RiskObjects to already be loaded (via Include).
/// </summary>
public static class ClaimDetailDtoFactory
{
    public static ClaimDetailDto Build(IMapper mapper, Claim claim, IReadOnlyList<ValidationIssue> issues)
    {
        var dto = mapper.Map<ClaimDetailDto>(claim);
        dto.LossEvent = claim.LossEvent is null ? null : mapper.Map<LossEventDto>(claim.LossEvent);
        dto.Parties = mapper.Map<List<ClaimPartyDto>>(claim.Parties);
        dto.RiskObjects = mapper.Map<List<ClaimRiskObjectDto>>(claim.RiskObjects);
        dto.ValidationIssues = issues
            .Select(i => new ValidationIssueDto(i.Code, i.Severity.ToString(), i.Message))
            .ToList();
        dto.ValidNextStatuses = ClaimStatusTransitionRules.GetValidNextStatuses(claim.Status)
            .Select(s => s.ToString())
            .ToList();
        return dto;
    }
}

using AutoMapper;
using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using ClaimsModule.Domain.Rules;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Claims.Commands.CreateClaim;

public class CreateClaimCommandHandler(
    IClaimsDbContext context,
    ICurrentUserService currentUser,
    IClaimNumberGenerator claimNumberGenerator,
    IClaimValidationService validationService,
    IAuditLogService auditLog,
    IMapper mapper) : IRequestHandler<CreateClaimCommand, ClaimDetailDto>
{
    public async Task<ClaimDetailDto> Handle(CreateClaimCommand request, CancellationToken cancellationToken)
    {
        var policy = request.PolicyId.HasValue
            ? await context.Policies.FirstOrDefaultAsync(p => p.PolicyId == request.PolicyId, cancellationToken)
            : null;

        var causeOfLossCode = await context.CauseOfLossCodes
            .FirstOrDefaultAsync(c => c.Code == request.CauseOfLossCode, cancellationToken);

        var claimNumber = await claimNumberGenerator.GenerateAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var claim = new Claim
        {
            ClaimId = Guid.NewGuid(),
            OrganisationId = currentUser.OrganisationId,
            ClaimNumber = claimNumber,
            PolicyId = policy?.PolicyId,
            PolicyNumber = policy?.PolicyNumber,
            ClientName = policy?.ClientName,
            Status = ClaimStatus.Draft,
            ReportedDate = now,
            CreatedAt = now,
            UserCreated = currentUser.UserId
        };

        var lossEvent = new LossEvent
        {
            LossEventId = Guid.NewGuid(),
            ClaimId = claim.ClaimId,
            OrganisationId = currentUser.OrganisationId,
            LossDate = request.LossDate,
            LossDescription = request.LossDescription,
            LossLocation = request.LossLocation,
            CauseOfLossCode = request.CauseOfLossCode,
            EstimatedLossAmount = request.EstimatedLossAmount,
            ReportDate = now,
            PoliceReportNumber = request.PoliceReportNumber,
            CreatedAt = now,
            UserCreated = currentUser.UserId
        };

        var parties = request.Parties.Select(p => new ClaimParty
        {
            ClaimPartyId = Guid.NewGuid(),
            ClaimId = claim.ClaimId,
            OrganisationId = currentUser.OrganisationId,
            PartyRole = p.PartyRole,
            PartyType = p.PartyType,
            FirstName = p.FirstName,
            LastName = p.LastName,
            CompanyName = p.CompanyName,
            Email = p.Email,
            Phone = p.Phone,
            IsActive = true,
            CreatedAt = now,
            UserCreated = currentUser.UserId
        }).ToList();

        var riskObjects = request.RiskObjects.Select(r => new ClaimRiskObject
        {
            ClaimRiskObjectId = Guid.NewGuid(),
            ClaimId = claim.ClaimId,
            OrganisationId = currentUser.OrganisationId,
            AssetType = r.AssetType,
            AssetDescription = r.AssetDescription,
            DamageDescription = r.DamageDescription,
            IsPrimary = r.IsPrimary,
            AssetReference = r.AssetReference,
            CreatedAt = now,
            UserCreated = currentUser.UserId
        }).ToList();

        context.Claims.Add(claim);
        context.LossEvents.Add(lossEvent);
        context.ClaimParties.AddRange(parties);
        context.ClaimRiskObjects.AddRange(riskObjects);

        await auditLog.LogAsync(claim.ClaimId, "CLAIM_CREATED", $"Claim {claimNumber} created.",
            cancellationToken: cancellationToken);

        var issues = validationService.Evaluate(claim, lossEvent, policy, causeOfLossCode, parties);
        foreach (var issue in issues)
        {
            await auditLog.LogAsync(claim.ClaimId, "VALIDATION_ISSUE_ADDED", issue.Message,
                cancellationToken: cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);

        var dto = mapper.Map<ClaimDetailDto>(claim);
        dto.LossEvent = mapper.Map<LossEventDto>(lossEvent);
        dto.Parties = mapper.Map<List<ClaimPartyDto>>(parties);
        dto.RiskObjects = mapper.Map<List<ClaimRiskObjectDto>>(riskObjects);
        dto.ValidationIssues = issues
            .Select(i => new ValidationIssueDto(i.Code, i.Severity.ToString(), i.Message))
            .ToList();
        dto.ValidNextStatuses = ClaimStatusTransitionRules.GetValidNextStatuses(claim.Status)
            .Select(s => s.ToString())
            .ToList();

        return dto;
    }
}

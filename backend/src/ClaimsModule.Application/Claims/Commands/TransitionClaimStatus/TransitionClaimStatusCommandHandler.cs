using AutoMapper;
using ClaimsModule.Application.Claims.Common;
using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using ClaimsModule.Domain.Rules;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Application.Claims.Commands.TransitionClaimStatus;

public class TransitionClaimStatusCommandHandler(
    IClaimsDbContext context,
    ICurrentUserService currentUser,
    IClaimValidationService validationService,
    IAuditLogService auditLog,
    IMapper mapper) : IRequestHandler<TransitionClaimStatusCommand, ClaimDetailDto>
{
    public async Task<ClaimDetailDto> Handle(TransitionClaimStatusCommand request, CancellationToken cancellationToken)
    {
        var claim = await context.Claims
            .Include(c => c.LossEvent)
            .Include(c => c.Parties)
            .Include(c => c.RiskObjects)
            .Include(c => c.ReserveComponents).ThenInclude(rc => rc.History)
            .FirstOrDefaultAsync(c => c.ClaimId == request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), request.ClaimId);

        if (!ClaimStatusTransitionRules.IsValidTransition(claim.Status, request.TargetStatus))
        {
            var valid = ClaimStatusTransitionRules.GetValidNextStatuses(claim.Status);
            throw new BusinessRuleException("StatusTransition",
                $"Transition from {claim.Status} to {request.TargetStatus} is not permitted." +
                (valid.Count > 0 ? $" Valid next statuses: {string.Join(", ", valid)}." : " No further transitions are possible."));
        }

        switch (request.TargetStatus)
        {
            case ClaimStatus.Open:
                await EnsureNoCriticalIssuesAsync(claim, cancellationToken); // BR-ST-02
                break;

            case ClaimStatus.Closed:
                await EnsureNoCriticalIssuesAsync(claim, cancellationToken); // CC-02
                EnsureClosureConditions(claim, request.Reason); // CC-01, CC-03, CC-04
                break;

            case ClaimStatus.PendingPayment:
                EnsureHasApprovedReserve(claim); // BR-ST: at least one approved reserve component
                break;

            case ClaimStatus.Withdrawn:
                if (string.IsNullOrWhiteSpace(request.Reason))
                    throw new BusinessRuleException("Reason", "Withdrawal reason code is required.");
                break;

            case ClaimStatus.Reopened:
                if (currentUser.Role is not ("supervisor" or "manager"))
                    throw new BusinessRuleException("Role", "Reopening a claim requires Supervisor role."); // BR-ST-04
                if (string.IsNullOrWhiteSpace(request.Reason))
                    throw new BusinessRuleException("Reason", "Reopen reason code is required.");
                break;
        }

        await ApplyTransitionAsync(claim, request.TargetStatus, request.Reason, cancellationToken);

        // FRS 4.2: Reopened -> Open happens immediately as a second step, both logged.
        if (request.TargetStatus == ClaimStatus.Reopened)
        {
            await ApplyTransitionAsync(claim, ClaimStatus.Open, null, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);

        var issues = validationService.Evaluate(claim, claim.LossEvent, await LoadPolicyAsync(claim, cancellationToken),
            await LoadCauseOfLossCodeAsync(claim, cancellationToken), claim.Parties);

        return ClaimDetailDtoFactory.Build(mapper, claim, issues);
    }

    private async Task ApplyTransitionAsync(Claim claim, ClaimStatus targetStatus, string? reason, CancellationToken cancellationToken)
    {
        var previousStatus = claim.Status;
        claim.Status = targetStatus;
        claim.UpdatedAt = DateTimeOffset.UtcNow;
        claim.UserModified = currentUser.UserId;

        if (targetStatus == ClaimStatus.Closed)
        {
            claim.ClosedAt = DateTimeOffset.UtcNow;
            claim.ClosureReason = reason;
        }

        await auditLog.LogAsync(claim.ClaimId, "STATUS_CHANGED",
            $"Status changed from {previousStatus} to {targetStatus}.",
            oldValue: previousStatus.ToString(), newValue: targetStatus.ToString(), cancellationToken: cancellationToken);

        if (targetStatus == ClaimStatus.Closed)
        {
            await auditLog.LogAsync(claim.ClaimId, "CLAIM_CLOSED", "Claim closed.",
                newValue: reason, cancellationToken: cancellationToken);
        }

        if (targetStatus == ClaimStatus.Reopened)
        {
            await auditLog.LogAsync(claim.ClaimId, "CLAIM_REOPENED", "Claim reopened.",
                newValue: reason, cancellationToken: cancellationToken);
        }
    }

    private async Task<Policy?> LoadPolicyAsync(Claim claim, CancellationToken cancellationToken) =>
        claim.PolicyId.HasValue
            ? await context.Policies.FirstOrDefaultAsync(p => p.PolicyId == claim.PolicyId, cancellationToken)
            : null;

    private async Task<CauseOfLossCode?> LoadCauseOfLossCodeAsync(Claim claim, CancellationToken cancellationToken) =>
        claim.LossEvent is null
            ? null
            : await context.CauseOfLossCodes.FirstOrDefaultAsync(c => c.Code == claim.LossEvent.CauseOfLossCode, cancellationToken);

    private async Task EnsureNoCriticalIssuesAsync(Claim claim, CancellationToken cancellationToken)
    {
        var policy = await LoadPolicyAsync(claim, cancellationToken);
        var causeOfLossCode = await LoadCauseOfLossCodeAsync(claim, cancellationToken);

        var critical = validationService
            .Evaluate(claim, claim.LossEvent, policy, causeOfLossCode, claim.Parties)
            .Where(i => i.Severity == ValidationSeverity.Critical)
            .ToList();

        if (critical.Count != 0)
        {
            throw new BusinessRuleException(critical
                .GroupBy(i => i.Code)
                .ToDictionary(g => g.Key, g => g.Select(i => i.Message).ToArray()));
        }
    }

    /// <summary>CC-01, CC-03, CC-04 (FRS 4.3). CC-02 is handled separately via EnsureNoCriticalIssuesAsync.</summary>
    private static void EnsureClosureConditions(Claim claim, string? justification)
    {
        // CC-01: no reserve components with a PendingApproval transaction remain.
        var hasPendingApproval = claim.ReserveComponents
            .SelectMany(rc => rc.History)
            .Any(h => h.ApprovalStatus == ReserveApprovalStatus.PendingApproval);
        if (hasPendingApproval)
        {
            throw new BusinessRuleException("Reserves",
                "Claim cannot be closed - one or more reserve components have a pending approval.");
        }

        // CC-03: at least one active Claimant party.
        if (!claim.Parties.Any(p => p.PartyRole == PartyRole.Claimant && p.IsActive))
        {
            throw new BusinessRuleException("Claimant",
                "Claim cannot be closed - at least one active Claimant party is required.");
        }

        // CC-04: non-zero reserve balance requires an explicit justification note.
        var hasOpenBalance = claim.ReserveComponents.Any(r => r.CurrentAmount > 0);
        if (hasOpenBalance && string.IsNullOrWhiteSpace(justification))
        {
            throw new BusinessRuleException("Justification",
                "Claim has open reserves - provide a justification note to confirm closure.");
        }
    }

    private static void EnsureHasApprovedReserve(Claim claim)
    {
        var hasApproved = claim.ReserveComponents
            .SelectMany(rc => rc.History)
            .Any(h => h.ApprovalStatus is ReserveApprovalStatus.Approved or ReserveApprovalStatus.AutoApproved);
        if (!hasApproved)
        {
            throw new BusinessRuleException("Reserve",
                "At least one approved reserve component is required before PendingPayment.");
        }
    }
}

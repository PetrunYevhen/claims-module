using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Application.Common.Services;

/// <summary>Implements FRS section 7.1 FNOL/claim creation rules (BR-C-01/02/03/05/06/07).</summary>
public class ClaimValidationService : IClaimValidationService
{
    public IReadOnlyList<ValidationIssue> Evaluate(
        Claim claim,
        LossEvent? lossEvent,
        Policy? policy,
        CauseOfLossCode? causeOfLossCode,
        IEnumerable<ClaimParty> parties)
    {
        var issues = new List<ValidationIssue>();

        if (lossEvent is not null)
        {
            // BR-C-01
            if (lossEvent.LossDate > DateTimeOffset.UtcNow)
            {
                issues.Add(new ValidationIssue("LossDate", ValidationSeverity.Critical,
                    "Loss date cannot be in the future."));
            }

            // BR-C-02
            if (policy is not null)
            {
                var lossDateOnly = DateOnly.FromDateTime(lossEvent.LossDate.UtcDateTime);
                if (lossDateOnly < policy.EffectiveDate || lossDateOnly > policy.ExpirationDate)
                {
                    issues.Add(new ValidationIssue("PolicyPeriod", ValidationSeverity.Warning,
                        "Loss date is outside the policy effective period."));
                }
            }

            // BR-C-07
            if (string.IsNullOrWhiteSpace(lossEvent.LossDescription) || lossEvent.LossDescription.Length < 20)
            {
                issues.Add(new ValidationIssue("LossDescription", ValidationSeverity.Critical,
                    "Loss description is required and must be at least 20 characters."));
            }
        }

        // BR-C-05
        if (causeOfLossCode is null || !causeOfLossCode.IsActive)
        {
            issues.Add(new ValidationIssue("CauseOfLossCode", ValidationSeverity.Critical,
                "Cause of loss code is not recognised or is inactive."));
        }

        // BR-C-06
        if (claim.PolicyId is null)
        {
            issues.Add(new ValidationIssue("PolicyId", ValidationSeverity.Warning,
                "No policy linked - claim requires policy association before financial actions are permitted."));
        }

        // BR-C-03 / BR-P-01
        if (!parties.Any(p => p.PartyRole == PartyRole.Claimant && p.IsActive))
        {
            issues.Add(new ValidationIssue("ClaimParties", ValidationSeverity.Critical,
                "At least one Claimant party is required to open a claim."));
        }

        return issues;
    }
}

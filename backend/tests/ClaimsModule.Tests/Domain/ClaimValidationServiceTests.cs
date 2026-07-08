using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Application.Common.Services;
using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using Xunit;

namespace ClaimsModule.Tests.Domain;

public class ClaimValidationServiceTests
{
    private readonly ClaimValidationService _sut = new();

    private static Claim Claim(Guid? policyId = null) => new()
    {
        ClaimId = Guid.NewGuid(),
        OrganisationId = SeedConstants.OrganisationId,
        ClaimNumber = "CLM-2026-0000001",
        PolicyId = policyId,
        Status = ClaimStatus.Draft,
        ReportedDate = DateTimeOffset.UtcNow,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static LossEvent LossEvent(DateTimeOffset lossDate, string description) => new()
    {
        LossEventId = Guid.NewGuid(),
        ClaimId = Guid.NewGuid(),
        OrganisationId = SeedConstants.OrganisationId,
        LossDate = lossDate,
        LossDescription = description,
        CauseOfLossCode = "COL-FIRE",
        ReportDate = DateTimeOffset.UtcNow
    };

    private static CauseOfLossCode ActiveCode() => new()
    {
        CauseOfLossCodeId = Guid.NewGuid(),
        Code = "COL-FIRE",
        Name = "Fire",
        PerilCategory = PerilCategory.Property,
        IsActive = true
    };

    private static ClaimParty Claimant() => new()
    {
        ClaimPartyId = Guid.NewGuid(),
        PartyRole = PartyRole.Claimant,
        PartyType = PartyType.Person,
        IsActive = true
    };

    private static Policy PolicyForYear2026() => new()
    {
        PolicyId = Guid.NewGuid(),
        PolicyNumber = "POL-2026-1",
        ClientName = "Test Co",
        EffectiveDate = new DateOnly(2026, 1, 1),
        ExpirationDate = new DateOnly(2026, 12, 31),
        Status = PolicyStatus.Active,
        CoverageTypes = "Property"
    };

    [Fact]
    public void Clean_claim_with_policy_and_claimant_has_no_issues()
    {
        var loss = LossEvent(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero), "A description well over twenty characters.");
        var issues = _sut.Evaluate(Claim(Guid.NewGuid()), loss, PolicyForYear2026(), ActiveCode(), [Claimant()]);

        Assert.Empty(issues);
    }

    [Fact]
    public void Future_loss_date_is_a_critical_issue() // BR-C-01
    {
        var loss = LossEvent(DateTimeOffset.UtcNow.AddDays(5), "A description well over twenty characters.");
        var issues = _sut.Evaluate(Claim(Guid.NewGuid()), loss, PolicyForYear2026(), ActiveCode(), [Claimant()]);

        Assert.Contains(issues, i => i.Code == "LossDate" && i.Severity == ValidationSeverity.Critical);
    }

    [Fact]
    public void Short_description_is_a_critical_issue() // BR-C-07
    {
        var loss = LossEvent(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero), "too short");
        var issues = _sut.Evaluate(Claim(Guid.NewGuid()), loss, PolicyForYear2026(), ActiveCode(), [Claimant()]);

        Assert.Contains(issues, i => i.Code == "LossDescription" && i.Severity == ValidationSeverity.Critical);
    }

    [Fact]
    public void Missing_claimant_is_a_critical_issue() // BR-C-03
    {
        var loss = LossEvent(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero), "A description well over twenty characters.");
        var issues = _sut.Evaluate(Claim(Guid.NewGuid()), loss, PolicyForYear2026(), ActiveCode(), []);

        Assert.Contains(issues, i => i.Code == "ClaimParties" && i.Severity == ValidationSeverity.Critical);
    }

    [Fact]
    public void Inactive_or_unknown_cause_code_is_a_critical_issue() // BR-C-05
    {
        var loss = LossEvent(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero), "A description well over twenty characters.");
        var issues = _sut.Evaluate(Claim(Guid.NewGuid()), loss, PolicyForYear2026(), causeOfLossCode: null, [Claimant()]);

        Assert.Contains(issues, i => i.Code == "CauseOfLossCode" && i.Severity == ValidationSeverity.Critical);
    }

    [Fact]
    public void No_linked_policy_is_a_warning_only() // BR-C-06
    {
        var loss = LossEvent(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero), "A description well over twenty characters.");
        var issues = _sut.Evaluate(Claim(policyId: null), loss, policy: null, ActiveCode(), [Claimant()]);

        Assert.Contains(issues, i => i.Code == "PolicyId" && i.Severity == ValidationSeverity.Warning);
        Assert.DoesNotContain(issues, i => i.Severity == ValidationSeverity.Critical);
    }

    [Fact]
    public void Loss_date_outside_policy_period_is_a_warning() // BR-C-02
    {
        var loss = LossEvent(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), "A description well over twenty characters.");
        var issues = _sut.Evaluate(Claim(Guid.NewGuid()), loss, PolicyForYear2026(), ActiveCode(), [Claimant()]);

        Assert.Contains(issues, i => i.Code == "PolicyPeriod" && i.Severity == ValidationSeverity.Warning);
    }
}

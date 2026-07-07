using FluentValidation;

namespace ClaimsModule.Application.Claims.Commands.CreateClaim;

/// <summary>
/// Structural request validation only (required fields, basic shape). Business rules like
/// "loss date not in future" or "description >= 20 chars" are Critical *validation issues*
/// per FRS 5.4 - they don't block creation, only the later Draft -> Open transition - so they
/// live in IClaimValidationService, evaluated by the handler, not here.
/// </summary>
public class CreateClaimCommandValidator : AbstractValidator<CreateClaimCommand>
{
    public CreateClaimCommandValidator()
    {
        RuleFor(x => x.LossDate).NotEmpty().WithMessage("Loss date is required.");
        RuleFor(x => x.LossDescription).NotEmpty().WithMessage("Loss description is required.");
        RuleFor(x => x.CauseOfLossCode).NotEmpty().WithMessage("Cause of loss code is required.");

        RuleForEach(x => x.Parties).ChildRules(party =>
        {
            party.RuleFor(p => p.PartyRole).IsInEnum();
            party.RuleFor(p => p.PartyType).IsInEnum();
        });

        RuleForEach(x => x.RiskObjects).ChildRules(risk =>
        {
            risk.RuleFor(r => r.AssetType).IsInEnum();
            risk.RuleFor(r => r.AssetDescription).NotEmpty();
        });
    }
}

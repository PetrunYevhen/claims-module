using ClaimsModule.Domain.Enums;
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

        // The optional initial reserve is validated with the same BR-R-01 amount rule as a
        // standalone reserve (see CreateReserveCommandValidator); the change reason is defaulted
        // in the handler, so it is not required here.
        When(x => x.InitialReserve is not null, () =>
        {
            RuleFor(x => x.InitialReserve!.Component).IsInEnum();
            RuleFor(x => x.InitialReserve!)
                .Must(ir => ir.Component == ReserveComponentType.SubrogationRecoverable
                    ? ir.Amount != 0
                    : ir.Amount > 0)
                .WithMessage("Reserve amount must be greater than zero.");
        });
    }
}

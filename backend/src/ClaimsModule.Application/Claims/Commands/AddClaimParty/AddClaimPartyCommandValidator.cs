using FluentValidation;

namespace ClaimsModule.Application.Claims.Commands.AddClaimParty;

public class AddClaimPartyCommandValidator : AbstractValidator<AddClaimPartyCommand>
{
    public AddClaimPartyCommandValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.PartyRole).IsInEnum();
        RuleFor(x => x.PartyType).IsInEnum();
    }
}

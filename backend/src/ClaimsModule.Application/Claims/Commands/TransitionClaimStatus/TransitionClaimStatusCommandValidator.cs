using FluentValidation;

namespace ClaimsModule.Application.Claims.Commands.TransitionClaimStatus;

public class TransitionClaimStatusCommandValidator : AbstractValidator<TransitionClaimStatusCommand>
{
    public TransitionClaimStatusCommandValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.TargetStatus).IsInEnum();
    }
}

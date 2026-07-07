using ClaimsModule.Domain.Enums;
using FluentValidation;

namespace ClaimsModule.Application.Reserves.Commands.CreateReserve;

public class CreateReserveCommandValidator : AbstractValidator<CreateReserveCommand>
{
    public CreateReserveCommandValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.Component).IsInEnum();
        RuleFor(x => x.ChangeReason).NotEmpty().WithMessage("Change reason is required.");

        // BR-R-01: amount must be > 0, except SubrogationRecoverable which may be negative (but not zero).
        RuleFor(x => x.Amount)
            .Must((command, amount) => command.Component == ReserveComponentType.SubrogationRecoverable
                ? amount != 0
                : amount > 0)
            .WithMessage("Reserve amount must be greater than zero.");
    }
}

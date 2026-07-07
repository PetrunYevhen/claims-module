using FluentValidation;

namespace ClaimsModule.Application.Reserves.Commands.RejectReserve;

public class RejectReserveCommandValidator : AbstractValidator<RejectReserveCommand>
{
    public RejectReserveCommandValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.ReserveHistoryId).NotEmpty();
        RuleFor(x => x.RejectionReason).NotEmpty().WithMessage("Rejection reason is required.");
    }
}

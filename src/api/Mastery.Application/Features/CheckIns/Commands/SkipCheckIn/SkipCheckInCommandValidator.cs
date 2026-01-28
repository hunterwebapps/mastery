using FluentValidation;

namespace Mastery.Application.Features.CheckIns.Commands.SkipCheckIn;

public sealed class SkipCheckInCommandValidator : AbstractValidator<SkipCheckInCommand>
{
    public SkipCheckInCommandValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty()
            .WithMessage("Check-in type is required.")
            .Must(t => t is "Morning" or "Evening")
            .WithMessage("Type must be Morning or Evening.");
    }
}

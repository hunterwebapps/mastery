using FluentValidation;

namespace Mastery.Application.Features.CheckIns.Commands.SubmitEveningCheckIn;

public sealed class SubmitEveningCheckInCommandValidator : AbstractValidator<SubmitEveningCheckInCommand>
{
    private static readonly string[] ValidBlockerCategories =
        ["TooTired", "NoTime", "Forgot", "Environment", "Conflict", "Sickness", "Other"];

    public SubmitEveningCheckInCommandValidator()
    {
        RuleFor(x => x.EnergyLevelPm)
            .InclusiveBetween(1, 5)
            .When(x => x.EnergyLevelPm.HasValue)
            .WithMessage("Evening energy level must be between 1 and 5.");

        RuleFor(x => x.StressLevel)
            .InclusiveBetween(1, 5)
            .When(x => x.StressLevel.HasValue)
            .WithMessage("Stress level must be between 1 and 5.");

        RuleFor(x => x.Reflection)
            .MaximumLength(1000)
            .WithMessage("Reflection cannot exceed 1000 characters.");

        RuleFor(x => x.BlockerCategory)
            .Must(cat => cat is null || ValidBlockerCategories.Contains(cat))
            .WithMessage("Invalid blocker category.");

        RuleFor(x => x.BlockerNote)
            .MaximumLength(500)
            .WithMessage("Blocker note cannot exceed 500 characters.");
    }
}

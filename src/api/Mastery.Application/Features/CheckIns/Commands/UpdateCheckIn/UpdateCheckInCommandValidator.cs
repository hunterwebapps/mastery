using FluentValidation;

namespace Mastery.Application.Features.CheckIns.Commands.UpdateCheckIn;

public sealed class UpdateCheckInCommandValidator : AbstractValidator<UpdateCheckInCommand>
{
    public UpdateCheckInCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Check-in ID is required.");

        RuleFor(x => x.EnergyLevel)
            .InclusiveBetween(1, 5)
            .When(x => x.EnergyLevel.HasValue)
            .WithMessage("Energy level must be between 1 and 5.");

        RuleFor(x => x.SelectedMode)
            .Must(mode => mode is null or "Full" or "Maintenance" or "Minimum")
            .WithMessage("Mode must be Full, Maintenance, or Minimum.");

        RuleFor(x => x.EnergyLevelPm)
            .InclusiveBetween(1, 5)
            .When(x => x.EnergyLevelPm.HasValue)
            .WithMessage("Evening energy level must be between 1 and 5.");

        RuleFor(x => x.StressLevel)
            .InclusiveBetween(1, 5)
            .When(x => x.StressLevel.HasValue)
            .WithMessage("Stress level must be between 1 and 5.");

        RuleFor(x => x.Intention)
            .MaximumLength(500)
            .When(x => x.Intention != null);

        RuleFor(x => x.Reflection)
            .MaximumLength(1000)
            .When(x => x.Reflection != null);

        RuleFor(x => x.BlockerNote)
            .MaximumLength(500)
            .When(x => x.BlockerNote != null);

        RuleFor(x => x.Top1FreeText)
            .MaximumLength(200)
            .When(x => x.Top1FreeText != null);
    }
}

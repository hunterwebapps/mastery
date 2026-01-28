using FluentValidation;

namespace Mastery.Application.Features.CheckIns.Commands.SubmitMorningCheckIn;

public sealed class SubmitMorningCheckInCommandValidator : AbstractValidator<SubmitMorningCheckInCommand>
{
    public SubmitMorningCheckInCommandValidator()
    {
        RuleFor(x => x.EnergyLevel)
            .InclusiveBetween(1, 5)
            .WithMessage("Energy level must be between 1 and 5.");

        RuleFor(x => x.SelectedMode)
            .NotEmpty()
            .WithMessage("Day mode is required.")
            .Must(mode => mode is "Full" or "Maintenance" or "Minimum")
            .WithMessage("Mode must be Full, Maintenance, or Minimum.");

        RuleFor(x => x.Top1Type)
            .Must(type => type is null or "Task" or "Habit" or "Project" or "FreeText")
            .WithMessage("Invalid Top 1 type.");

        RuleFor(x => x.Top1FreeText)
            .MaximumLength(200)
            .WithMessage("Top 1 text cannot exceed 200 characters.");

        RuleFor(x => x.Top1FreeText)
            .NotEmpty()
            .When(x => x.Top1Type == "FreeText")
            .WithMessage("Free text is required when Top 1 type is FreeText.");

        RuleFor(x => x.Top1EntityId)
            .NotEmpty()
            .When(x => x.Top1Type is "Task" or "Habit" or "Project")
            .WithMessage("Entity ID is required for Task, Habit, or Project Top 1.");

        RuleFor(x => x.Intention)
            .MaximumLength(500)
            .WithMessage("Intention cannot exceed 500 characters.");
    }
}

using FluentValidation;
using Mastery.Domain.Enums;

namespace Mastery.Application.Features.Habits.Commands.CompleteOccurrence;

public sealed class CompleteOccurrenceCommandValidator : AbstractValidator<CompleteOccurrenceCommand>
{
    public CompleteOccurrenceCommandValidator()
    {
        RuleFor(x => x.HabitId)
            .NotEmpty().WithMessage("Habit ID is required.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required.")
            .Must(BeValidDate).WithMessage("Date must be a valid date in YYYY-MM-DD format.");

        When(x => !string.IsNullOrEmpty(x.Mode), () =>
        {
            RuleFor(x => x.Mode)
                .Must(BeValidHabitMode).WithMessage("Invalid mode. Valid modes: Full, Maintenance, Minimum.");
        });

        When(x => !string.IsNullOrEmpty(x.Note), () =>
        {
            RuleFor(x => x.Note)
                .MaximumLength(500).WithMessage("Note cannot exceed 500 characters.");
        });
    }

    private static bool BeValidDate(string date) => DateOnly.TryParse(date, out _);
    private static bool BeValidHabitMode(string? mode) => mode != null && Enum.TryParse<HabitMode>(mode, out _);
}

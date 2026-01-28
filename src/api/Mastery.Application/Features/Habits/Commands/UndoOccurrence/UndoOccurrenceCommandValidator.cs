using FluentValidation;

namespace Mastery.Application.Features.Habits.Commands.UndoOccurrence;

public sealed class UndoOccurrenceCommandValidator : AbstractValidator<UndoOccurrenceCommand>
{
    public UndoOccurrenceCommandValidator()
    {
        RuleFor(x => x.HabitId)
            .NotEmpty().WithMessage("Habit ID is required.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required.")
            .Must(BeValidDate).WithMessage("Date must be a valid date in YYYY-MM-DD format.");
    }

    private static bool BeValidDate(string date) => DateOnly.TryParse(date, out _);
}

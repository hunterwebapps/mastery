using FluentValidation;

namespace Mastery.Application.Features.Habits.Commands.SkipOccurrence;

public sealed class SkipOccurrenceCommandValidator : AbstractValidator<SkipOccurrenceCommand>
{
    public SkipOccurrenceCommandValidator()
    {
        RuleFor(x => x.HabitId)
            .NotEmpty().WithMessage("Habit ID is required.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required.")
            .Must(BeValidDate).WithMessage("Date must be a valid date in YYYY-MM-DD format.");

        When(x => !string.IsNullOrEmpty(x.Reason), () =>
        {
            RuleFor(x => x.Reason)
                .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");
        });
    }

    private static bool BeValidDate(string date) => DateOnly.TryParse(date, out _);
}

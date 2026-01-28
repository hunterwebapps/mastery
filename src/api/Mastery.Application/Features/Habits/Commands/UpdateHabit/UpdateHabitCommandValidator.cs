using FluentValidation;
using Mastery.Application.Features.Habits.Commands.CreateHabit;
using Mastery.Domain.Enums;

namespace Mastery.Application.Features.Habits.Commands.UpdateHabit;

public sealed class UpdateHabitCommandValidator : AbstractValidator<UpdateHabitCommand>
{
    public UpdateHabitCommandValidator()
    {
        RuleFor(x => x.HabitId)
            .NotEmpty().WithMessage("Habit ID is required.");

        When(x => x.Title != null, () =>
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Habit title cannot be empty if provided.")
                .MaximumLength(200).WithMessage("Habit title cannot exceed 200 characters.");
        });

        When(x => x.Description != null, () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");
        });

        When(x => x.Why != null, () =>
        {
            RuleFor(x => x.Why)
                .MaximumLength(500).WithMessage("Why cannot exceed 500 characters.");
        });

        When(x => !string.IsNullOrEmpty(x.DefaultMode), () =>
        {
            RuleFor(x => x.DefaultMode)
                .Must(BeValidHabitMode).WithMessage("Invalid default mode. Valid modes: Full, Maintenance, Minimum.");
        });

        When(x => x.Schedule != null, () =>
        {
            RuleFor(x => x.Schedule)
                .SetValidator(new CreateHabitScheduleInputValidator()!);
        });

        When(x => x.Policy != null, () =>
        {
            RuleFor(x => x.Policy)
                .SetValidator(new CreateHabitPolicyInputValidator()!);
        });
    }

    private static bool BeValidHabitMode(string? mode) => mode != null && Enum.TryParse<HabitMode>(mode, out _);
}

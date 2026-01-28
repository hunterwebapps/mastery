using FluentValidation;
using Mastery.Domain.Enums;

namespace Mastery.Application.Features.Habits.Commands.UpdateHabitStatus;

public sealed class UpdateHabitStatusCommandValidator : AbstractValidator<UpdateHabitStatusCommand>
{
    public UpdateHabitStatusCommandValidator()
    {
        RuleFor(x => x.HabitId)
            .NotEmpty().WithMessage("Habit ID is required.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(BeValidHabitStatus).WithMessage("Invalid status. Valid statuses: Active, Paused, Archived.");
    }

    private static bool BeValidHabitStatus(string status) => Enum.TryParse<HabitStatus>(status, out _);
}

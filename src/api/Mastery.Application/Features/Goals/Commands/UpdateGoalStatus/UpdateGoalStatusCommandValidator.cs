using FluentValidation;
using Mastery.Domain.Enums;

namespace Mastery.Application.Features.Goals.Commands.UpdateGoalStatus;

public sealed class UpdateGoalStatusCommandValidator : AbstractValidator<UpdateGoalStatusCommand>
{
    public UpdateGoalStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Goal ID is required.");

        RuleFor(x => x.NewStatus)
            .NotEmpty().WithMessage("New status is required.")
            .Must(BeValidStatus).WithMessage("Invalid status. Valid statuses: Active, Paused, Completed, Archived.");

        When(x => x.NewStatus == "Completed" && x.CompletionNotes != null, () =>
        {
            RuleFor(x => x.CompletionNotes)
                .MaximumLength(2000).WithMessage("Completion notes cannot exceed 2000 characters.");
        });
    }

    private static bool BeValidStatus(string status)
    {
        if (!Enum.TryParse<GoalStatus>(status, out var parsed))
            return false;

        // Can't directly set to Draft
        return parsed != GoalStatus.Draft;
    }
}

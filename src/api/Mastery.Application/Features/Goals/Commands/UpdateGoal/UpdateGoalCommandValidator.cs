using FluentValidation;

namespace Mastery.Application.Features.Goals.Commands.UpdateGoal;

public sealed class UpdateGoalCommandValidator : AbstractValidator<UpdateGoalCommand>
{
    public UpdateGoalCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Goal ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Goal title is required.")
            .MaximumLength(200).WithMessage("Goal title cannot exceed 200 characters.");

        When(x => x.Description != null, () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");
        });

        When(x => x.Why != null, () =>
        {
            RuleFor(x => x.Why)
                .MaximumLength(1000).WithMessage("Why cannot exceed 1000 characters.");
        });

        RuleFor(x => x.Priority)
            .InclusiveBetween(1, 5).WithMessage("Priority must be between 1 and 5.");
    }
}

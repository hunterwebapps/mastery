using FluentValidation;

namespace Mastery.Application.Features.Goals.Commands.DeleteGoal;

public sealed class DeleteGoalCommandValidator : AbstractValidator<DeleteGoalCommand>
{
    public DeleteGoalCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Goal ID is required.");
    }
}

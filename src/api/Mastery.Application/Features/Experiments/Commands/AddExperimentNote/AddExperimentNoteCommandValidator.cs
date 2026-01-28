using FluentValidation;

namespace Mastery.Application.Features.Experiments.Commands.AddExperimentNote;

public sealed class AddExperimentNoteCommandValidator : AbstractValidator<AddExperimentNoteCommand>
{
    public AddExperimentNoteCommandValidator()
    {
        RuleFor(x => x.ExperimentId)
            .NotEmpty().WithMessage("Experiment ID is required.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Note content is required.")
            .MaximumLength(2000).WithMessage("Note content cannot exceed 2000 characters.");
    }
}

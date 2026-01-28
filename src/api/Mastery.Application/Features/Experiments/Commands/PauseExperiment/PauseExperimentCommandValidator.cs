using FluentValidation;

namespace Mastery.Application.Features.Experiments.Commands.PauseExperiment;

public sealed class PauseExperimentCommandValidator : AbstractValidator<PauseExperimentCommand>
{
    public PauseExperimentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Experiment ID is required.");
    }
}

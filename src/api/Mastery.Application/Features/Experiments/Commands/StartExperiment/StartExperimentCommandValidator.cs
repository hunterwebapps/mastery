using FluentValidation;

namespace Mastery.Application.Features.Experiments.Commands.StartExperiment;

public sealed class StartExperimentCommandValidator : AbstractValidator<StartExperimentCommand>
{
    public StartExperimentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Experiment ID is required.");
    }
}

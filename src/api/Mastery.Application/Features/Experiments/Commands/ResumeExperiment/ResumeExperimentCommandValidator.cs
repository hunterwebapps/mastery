using FluentValidation;

namespace Mastery.Application.Features.Experiments.Commands.ResumeExperiment;

public sealed class ResumeExperimentCommandValidator : AbstractValidator<ResumeExperimentCommand>
{
    public ResumeExperimentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Experiment ID is required.");
    }
}

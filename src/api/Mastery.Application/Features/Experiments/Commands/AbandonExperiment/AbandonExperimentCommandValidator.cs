using FluentValidation;

namespace Mastery.Application.Features.Experiments.Commands.AbandonExperiment;

public sealed class AbandonExperimentCommandValidator : AbstractValidator<AbandonExperimentCommand>
{
    public AbandonExperimentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Experiment ID is required.");

        When(x => x.Reason != null, () =>
        {
            RuleFor(x => x.Reason)
                .MaximumLength(2000).WithMessage("Reason cannot exceed 2000 characters.");
        });
    }
}

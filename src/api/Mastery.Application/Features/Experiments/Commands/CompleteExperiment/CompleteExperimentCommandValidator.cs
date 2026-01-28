using FluentValidation;
using Mastery.Domain.Enums;

namespace Mastery.Application.Features.Experiments.Commands.CompleteExperiment;

public sealed class CompleteExperimentCommandValidator : AbstractValidator<CompleteExperimentCommand>
{
    public CompleteExperimentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Experiment ID is required.");

        RuleFor(x => x.OutcomeClassification)
            .NotEmpty().WithMessage("Outcome classification is required.")
            .Must(BeValidOutcome).WithMessage("Invalid outcome classification. Valid values: Positive, Neutral, Negative, Inconclusive.");

        When(x => x.ComplianceRate.HasValue, () =>
        {
            RuleFor(x => x.ComplianceRate)
                .InclusiveBetween(0m, 1m).WithMessage("Compliance rate must be between 0 and 1.");
        });

        When(x => x.NarrativeSummary != null, () =>
        {
            RuleFor(x => x.NarrativeSummary)
                .MaximumLength(4000).WithMessage("Narrative summary cannot exceed 4000 characters.");
        });
    }

    private static bool BeValidOutcome(string outcome) => Enum.TryParse<ExperimentOutcome>(outcome, out _);
}

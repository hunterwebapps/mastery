using FluentValidation;
using Mastery.Application.Features.Goals.Commands.CreateGoal;
using Mastery.Domain.Enums;

namespace Mastery.Application.Features.Goals.Commands.UpdateGoalScoreboard;

public sealed class UpdateGoalScoreboardCommandValidator : AbstractValidator<UpdateGoalScoreboardCommand>
{
    public UpdateGoalScoreboardCommandValidator()
    {
        RuleFor(x => x.GoalId)
            .NotEmpty().WithMessage("Goal ID is required.");

        RuleFor(x => x.Metrics)
            .NotNull().WithMessage("Metrics list is required.");

        When(x => x.Metrics != null && x.Metrics.Count > 0, () =>
        {
            RuleForEach(x => x.Metrics).SetValidator(new UpdateGoalMetricInputValidator());
        });
    }
}

public sealed class UpdateGoalMetricInputValidator : AbstractValidator<UpdateGoalMetricInput>
{
    public UpdateGoalMetricInputValidator()
    {
        RuleFor(x => x.MetricDefinitionId)
            .NotEmpty().WithMessage("Metric definition ID is required.");

        RuleFor(x => x.Kind)
            .NotEmpty().WithMessage("Metric kind is required.")
            .Must(BeValidMetricKind).WithMessage("Invalid metric kind. Valid kinds: Lag, Lead, Constraint.");

        RuleFor(x => x.Target)
            .NotNull().WithMessage("Target is required.")
            .SetValidator(new CreateTargetInputValidator());

        RuleFor(x => x.EvaluationWindow)
            .NotNull().WithMessage("Evaluation window is required.")
            .SetValidator(new CreateEvaluationWindowInputValidator());

        RuleFor(x => x.Aggregation)
            .NotEmpty().WithMessage("Aggregation is required.")
            .Must(BeValidAggregation).WithMessage("Invalid aggregation type.");

        RuleFor(x => x.SourceHint)
            .NotEmpty().WithMessage("Source hint is required.")
            .Must(BeValidSourceType).WithMessage("Invalid source type.");

        RuleFor(x => x.Weight)
            .InclusiveBetween(0.01m, 10m).WithMessage("Weight must be between 0.01 and 10.");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be non-negative.");
    }

    private static bool BeValidMetricKind(string kind) => Enum.TryParse<MetricKind>(kind, out _);
    private static bool BeValidAggregation(string aggregation) => Enum.TryParse<MetricAggregation>(aggregation, out _);
    private static bool BeValidSourceType(string sourceType) => Enum.TryParse<MetricSourceType>(sourceType, out _);
}

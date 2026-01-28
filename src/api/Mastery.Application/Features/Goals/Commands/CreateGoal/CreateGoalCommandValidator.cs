using FluentValidation;
using Mastery.Domain.Enums;

namespace Mastery.Application.Features.Goals.Commands.CreateGoal;

public sealed class CreateGoalCommandValidator : AbstractValidator<CreateGoalCommand>
{
    public CreateGoalCommandValidator()
    {
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

        When(x => x.Metrics != null && x.Metrics.Count > 0, () =>
        {
            RuleForEach(x => x.Metrics).SetValidator(new CreateGoalMetricInputValidator());
        });
    }
}

public sealed class CreateGoalMetricInputValidator : AbstractValidator<CreateGoalMetricInput>
{
    public CreateGoalMetricInputValidator()
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
            .Must(BeValidAggregation).WithMessage("Invalid aggregation. Valid types: Sum, Average, Max, Min, Count, Latest.");

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

public sealed class CreateTargetInputValidator : AbstractValidator<CreateTargetInput>
{
    public CreateTargetInputValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Target type is required.")
            .Must(BeValidTargetType).WithMessage("Invalid target type. Valid types: AtLeast, AtMost, Between, Exactly.");

        RuleFor(x => x.Value)
            .NotNull().WithMessage("Target value is required.");

        When(x => x.Type == "Between", () =>
        {
            RuleFor(x => x.MaxValue)
                .NotNull().WithMessage("MaxValue is required for Between target type.")
                .GreaterThan(x => x.Value).WithMessage("MaxValue must be greater than Value.");
        });
    }

    private static bool BeValidTargetType(string type) => Enum.TryParse<TargetType>(type, out _);
}

public sealed class CreateEvaluationWindowInputValidator : AbstractValidator<CreateEvaluationWindowInput>
{
    public CreateEvaluationWindowInputValidator()
    {
        RuleFor(x => x.WindowType)
            .NotEmpty().WithMessage("Window type is required.")
            .Must(BeValidWindowType).WithMessage("Invalid window type. Valid types: Daily, Weekly, Monthly, Rolling.");

        When(x => x.WindowType == "Rolling", () =>
        {
            RuleFor(x => x.RollingDays)
                .NotNull().WithMessage("RollingDays is required for Rolling window type.")
                .InclusiveBetween(1, 365).WithMessage("RollingDays must be between 1 and 365.");
        });

        When(x => x.StartDay != null, () =>
        {
            RuleFor(x => x.StartDay)
                .InclusiveBetween(0, 28).WithMessage("StartDay must be between 0 and 28.");
        });
    }

    private static bool BeValidWindowType(string type) => Enum.TryParse<WindowType>(type, out _);
}

using FluentValidation;
using Mastery.Domain.Enums;

namespace Mastery.Application.Features.Goals.Commands.AddGoalMetric;

public sealed class AddGoalMetricCommandValidator : AbstractValidator<AddGoalMetricCommand>
{
    public AddGoalMetricCommandValidator()
    {
        RuleFor(x => x.GoalId)
            .NotEmpty().WithMessage("Goal ID is required.");

        // Either existing ID or new name must be provided
        RuleFor(x => x)
            .Must(x => x.ExistingMetricDefinitionId.HasValue || !string.IsNullOrWhiteSpace(x.NewMetricName))
            .WithMessage("Must provide either existingMetricDefinitionId or newMetricName.");

        RuleFor(x => x.Kind)
            .NotEmpty().WithMessage("Metric kind is required.")
            .Must(x => Enum.TryParse<MetricKind>(x, out _))
            .WithMessage("Kind must be Lag, Lead, or Constraint.");

        RuleFor(x => x.TargetType)
            .NotEmpty().WithMessage("Target type is required.")
            .Must(x => Enum.TryParse<TargetType>(x, out _))
            .WithMessage("TargetType must be AtLeast, AtMost, Between, or Exactly.");

        RuleFor(x => x.TargetMaxValue)
            .NotNull()
            .When(x => x.TargetType == "Between")
            .WithMessage("TargetMaxValue is required for Between target type.");

        RuleFor(x => x.WindowType)
            .NotEmpty().WithMessage("Window type is required.")
            .Must(x => Enum.TryParse<WindowType>(x, out _))
            .WithMessage("WindowType must be Daily, Weekly, Monthly, or Rolling.");

        RuleFor(x => x.RollingDays)
            .NotNull()
            .InclusiveBetween(1, 365)
            .When(x => x.WindowType == "Rolling")
            .WithMessage("RollingDays (1-365) is required for Rolling window type.");

        RuleFor(x => x.Aggregation)
            .NotEmpty().WithMessage("Aggregation is required.")
            .Must(x => Enum.TryParse<MetricAggregation>(x, out _))
            .WithMessage("Aggregation must be Sum, Average, Max, Min, Count, or Latest.");

        RuleFor(x => x.SourceHint)
            .NotEmpty().WithMessage("Source hint is required.")
            .Must(x => Enum.TryParse<MetricSourceType>(x, out _))
            .WithMessage("SourceHint must be Manual, Integrated, or Computed.");

        RuleFor(x => x.Weight)
            .InclusiveBetween(0m, 1m)
            .WithMessage("Weight must be between 0 and 1.");
    }
}

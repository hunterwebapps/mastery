using FluentValidation;
using Mastery.Domain.Enums;

namespace Mastery.Application.Features.Metrics.Commands.UpdateMetricDefinition;

public sealed class UpdateMetricDefinitionCommandValidator : AbstractValidator<UpdateMetricDefinitionCommand>
{
    public UpdateMetricDefinitionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Metric definition ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Metric name is required.")
            .MaximumLength(100).WithMessage("Metric name cannot exceed 100 characters.");

        When(x => x.Description != null, () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
        });

        RuleFor(x => x.DataType)
            .NotEmpty().WithMessage("Data type is required.")
            .Must(BeValidDataType).WithMessage("Invalid data type.");

        RuleFor(x => x.Direction)
            .NotEmpty().WithMessage("Direction is required.")
            .Must(BeValidDirection).WithMessage("Invalid direction.");

        RuleFor(x => x.DefaultCadence)
            .NotEmpty().WithMessage("Default cadence is required.")
            .Must(BeValidCadence).WithMessage("Invalid cadence.");

        RuleFor(x => x.DefaultAggregation)
            .NotEmpty().WithMessage("Default aggregation is required.")
            .Must(BeValidAggregation).WithMessage("Invalid aggregation.");

        When(x => x.Unit != null, () =>
        {
            RuleFor(x => x.Unit!.Type)
                .NotEmpty().WithMessage("Unit type is required.")
                .MaximumLength(50).WithMessage("Unit type cannot exceed 50 characters.");

            RuleFor(x => x.Unit!.Label)
                .NotEmpty().WithMessage("Unit label is required.")
                .MaximumLength(20).WithMessage("Unit label cannot exceed 20 characters.");
        });

        When(x => x.Tags != null, () =>
        {
            RuleForEach(x => x.Tags)
                .MaximumLength(50).WithMessage("Tag cannot exceed 50 characters.");
        });
    }

    private static bool BeValidDataType(string type) => Enum.TryParse<MetricDataType>(type, out _);
    private static bool BeValidDirection(string direction) => Enum.TryParse<MetricDirection>(direction, out _);
    private static bool BeValidCadence(string cadence) => Enum.TryParse<WindowType>(cadence, out _);
    private static bool BeValidAggregation(string aggregation) => Enum.TryParse<MetricAggregation>(aggregation, out _);
}

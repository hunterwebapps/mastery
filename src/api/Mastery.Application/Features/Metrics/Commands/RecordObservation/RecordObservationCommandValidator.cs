using FluentValidation;
using Mastery.Domain.Enums;

namespace Mastery.Application.Features.Metrics.Commands.RecordObservation;

public sealed class RecordObservationCommandValidator : AbstractValidator<RecordObservationCommand>
{
    public RecordObservationCommandValidator()
    {
        RuleFor(x => x.MetricDefinitionId)
            .NotEmpty().WithMessage("Metric definition ID is required.");

        RuleFor(x => x.Value)
            .NotNull().WithMessage("Value is required.");

        RuleFor(x => x.Source)
            .NotEmpty().WithMessage("Source is required.")
            .Must(BeValidSource).WithMessage("Invalid source type. Valid types: Manual, Habit, Task, CheckIn, Integration, Computed.");

        When(x => x.CorrelationId != null, () =>
        {
            RuleFor(x => x.CorrelationId)
                .MaximumLength(100).WithMessage("Correlation ID cannot exceed 100 characters.");
        });

        When(x => x.Note != null, () =>
        {
            RuleFor(x => x.Note)
                .MaximumLength(500).WithMessage("Note cannot exceed 500 characters.");
        });
    }

    private static bool BeValidSource(string source) => Enum.TryParse<MetricSourceType>(source, out _);
}

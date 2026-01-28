using FluentValidation;
using Mastery.Domain.Enums;

namespace Mastery.Application.Features.Experiments.Commands.CreateExperiment;

public sealed class CreateExperimentCommandValidator : AbstractValidator<CreateExperimentCommand>
{
    public CreateExperimentCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Experiment title is required.")
            .MaximumLength(200).WithMessage("Experiment title cannot exceed 200 characters.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .Must(BeValidCategory).WithMessage("Invalid experiment category. Valid categories: Habit, Routine, Environment, Mindset, Productivity, Health, Social, PlanRealism, FrictionReduction, CheckInConsistency, Top1FollowThrough, Other.");

        RuleFor(x => x.CreatedFrom)
            .NotEmpty().WithMessage("CreatedFrom is required.")
            .Must(BeValidCreatedFrom).WithMessage("Invalid created from. Valid values: Manual, WeeklyReview, Diagnostic, Coaching.")
;

        RuleFor(x => x.Hypothesis)
            .NotNull().WithMessage("Hypothesis is required.")
            .SetValidator(new CreateHypothesisInputValidator());

        RuleFor(x => x.MeasurementPlan)
            .NotNull().WithMessage("Measurement plan is required.")
            .SetValidator(new CreateMeasurementPlanInputValidator());

        When(x => x.Description != null, () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");
        });
    }

    private static bool BeValidCategory(string category) => Enum.TryParse<ExperimentCategory>(category, out _);
    private static bool BeValidCreatedFrom(string createdFrom) => Enum.TryParse<ExperimentCreatedFrom>(createdFrom, out _);
}

public sealed class CreateHypothesisInputValidator : AbstractValidator<CreateHypothesisInput>
{
    public CreateHypothesisInputValidator()
    {
        RuleFor(x => x.Change)
            .NotEmpty().WithMessage("Hypothesis change is required.")
            .MaximumLength(500).WithMessage("Hypothesis change cannot exceed 500 characters.");

        RuleFor(x => x.ExpectedOutcome)
            .NotEmpty().WithMessage("Hypothesis expected outcome is required.")
            .MaximumLength(500).WithMessage("Hypothesis expected outcome cannot exceed 500 characters.");

        When(x => x.Rationale != null, () =>
        {
            RuleFor(x => x.Rationale)
                .MaximumLength(1000).WithMessage("Hypothesis rationale cannot exceed 1000 characters.");
        });
    }
}

public sealed class CreateMeasurementPlanInputValidator : AbstractValidator<CreateMeasurementPlanInput>
{
    public CreateMeasurementPlanInputValidator()
    {
        RuleFor(x => x.PrimaryMetricDefinitionId)
            .NotEmpty().WithMessage("Primary metric definition ID is required.");

        RuleFor(x => x.PrimaryAggregation)
            .NotEmpty().WithMessage("Primary aggregation is required.")
            .Must(BeValidAggregation).WithMessage("Invalid aggregation. Valid types: Sum, Average, Max, Min, Count, Latest.");

        RuleFor(x => x.BaselineWindowDays)
            .InclusiveBetween(1, 90).WithMessage("Baseline window days must be between 1 and 90.");

        RuleFor(x => x.RunWindowDays)
            .InclusiveBetween(1, 90).WithMessage("Run window days must be between 1 and 90.");

        RuleFor(x => x.MinComplianceThreshold)
            .InclusiveBetween(0m, 1m).WithMessage("Minimum compliance threshold must be between 0 and 1.");
    }

    private static bool BeValidAggregation(string aggregation) => Enum.TryParse<MetricAggregation>(aggregation, out _);
}

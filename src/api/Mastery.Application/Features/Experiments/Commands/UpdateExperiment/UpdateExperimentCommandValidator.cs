using FluentValidation;
using Mastery.Application.Features.Experiments.Commands.CreateExperiment;
using Mastery.Domain.Enums;

namespace Mastery.Application.Features.Experiments.Commands.UpdateExperiment;

public sealed class UpdateExperimentCommandValidator : AbstractValidator<UpdateExperimentCommand>
{
    public UpdateExperimentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Experiment ID is required.");

        When(x => x.Title != null, () =>
        {
            RuleFor(x => x.Title)
                .MaximumLength(200).WithMessage("Experiment title cannot exceed 200 characters.");
        });

        When(x => x.Description != null, () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");
        });

        When(x => x.Category != null, () =>
        {
            RuleFor(x => x.Category)
                .Must(BeValidCategory!).WithMessage("Invalid experiment category. Valid categories: Habit, Routine, Environment, Mindset, Productivity, Health, Social, PlanRealism, FrictionReduction, CheckInConsistency, Top1FollowThrough, Other.");
        });

        When(x => x.Hypothesis != null, () =>
        {
            RuleFor(x => x.Hypothesis)
                .SetValidator(new CreateHypothesisInputValidator()!);
        });

        When(x => x.MeasurementPlan != null, () =>
        {
            RuleFor(x => x.MeasurementPlan)
                .SetValidator(new CreateMeasurementPlanInputValidator()!);
        });
    }

    private static bool BeValidCategory(string category) => Enum.TryParse<ExperimentCategory>(category, out _);
}

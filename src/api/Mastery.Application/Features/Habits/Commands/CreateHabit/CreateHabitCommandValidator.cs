using FluentValidation;
using Mastery.Domain.Enums;

namespace Mastery.Application.Features.Habits.Commands.CreateHabit;

public sealed class CreateHabitCommandValidator : AbstractValidator<CreateHabitCommand>
{
    public CreateHabitCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Habit title is required.")
            .MaximumLength(200).WithMessage("Habit title cannot exceed 200 characters.");

        When(x => x.Description != null, () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");
        });

        When(x => x.Why != null, () =>
        {
            RuleFor(x => x.Why)
                .MaximumLength(500).WithMessage("Why cannot exceed 500 characters.");
        });

        RuleFor(x => x.DefaultMode)
            .NotEmpty().WithMessage("Default mode is required.")
            .Must(BeValidHabitMode).WithMessage("Invalid default mode. Valid modes: Full, Maintenance, Minimum.");

        RuleFor(x => x.Schedule)
            .NotNull().WithMessage("Schedule is required.")
            .SetValidator(new CreateHabitScheduleInputValidator());

        When(x => x.Policy != null, () =>
        {
            RuleFor(x => x.Policy)
                .SetValidator(new CreateHabitPolicyInputValidator()!);
        });

        When(x => x.MetricBindings != null && x.MetricBindings.Count > 0, () =>
        {
            RuleForEach(x => x.MetricBindings).SetValidator(new CreateHabitMetricBindingInputValidator());
        });

        When(x => x.Variants != null && x.Variants.Count > 0, () =>
        {
            RuleForEach(x => x.Variants).SetValidator(new CreateHabitVariantInputValidator());
        });
    }

    private static bool BeValidHabitMode(string mode) => Enum.TryParse<HabitMode>(mode, out _);
}

public sealed class CreateHabitScheduleInputValidator : AbstractValidator<CreateHabitScheduleInput>
{
    public CreateHabitScheduleInputValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Schedule type is required.")
            .Must(BeValidScheduleType).WithMessage("Invalid schedule type. Valid types: Daily, DaysOfWeek, WeeklyFrequency, Interval.");

        When(x => x.Type == "DaysOfWeek", () =>
        {
            RuleFor(x => x.DaysOfWeek)
                .NotEmpty().WithMessage("Days of week are required for DaysOfWeek schedule type.")
                .Must(days => days!.All(d => d >= 0 && d <= 6))
                .WithMessage("Days of week must be between 0 (Sunday) and 6 (Saturday).");
        });

        When(x => x.Type == "WeeklyFrequency", () =>
        {
            RuleFor(x => x.FrequencyPerWeek)
                .NotNull().WithMessage("Frequency per week is required for WeeklyFrequency schedule type.")
                .InclusiveBetween(1, 7).WithMessage("Frequency per week must be between 1 and 7.");
        });

        When(x => x.Type == "Interval", () =>
        {
            RuleFor(x => x.IntervalDays)
                .NotNull().WithMessage("Interval days is required for Interval schedule type.")
                .InclusiveBetween(1, 90).WithMessage("Interval days must be between 1 and 90.");
        });

        When(x => !string.IsNullOrEmpty(x.StartDate), () =>
        {
            RuleFor(x => x.StartDate)
                .Must(BeValidDate).WithMessage("Start date must be a valid date in YYYY-MM-DD format.");
        });

        When(x => !string.IsNullOrEmpty(x.EndDate), () =>
        {
            RuleFor(x => x.EndDate)
                .Must(BeValidDate).WithMessage("End date must be a valid date in YYYY-MM-DD format.");
        });
    }

    private static bool BeValidScheduleType(string type) => Enum.TryParse<ScheduleType>(type, out _);
    private static bool BeValidDate(string? date) => date != null && DateOnly.TryParse(date, out _);
}

public sealed class CreateHabitPolicyInputValidator : AbstractValidator<CreateHabitPolicyInput>
{
    public CreateHabitPolicyInputValidator()
    {
        RuleFor(x => x.MaxBackfillDays)
            .InclusiveBetween(0, 30).WithMessage("Max backfill days must be between 0 and 30.");

        When(x => !string.IsNullOrEmpty(x.LateCutoffTime), () =>
        {
            RuleFor(x => x.LateCutoffTime)
                .Must(BeValidTime).WithMessage("Late cutoff time must be a valid time in HH:mm format.");
        });
    }

    private static bool BeValidTime(string? time) => time != null && TimeOnly.TryParse(time, out _);
}

public sealed class CreateHabitMetricBindingInputValidator : AbstractValidator<CreateHabitMetricBindingInput>
{
    public CreateHabitMetricBindingInputValidator()
    {
        RuleFor(x => x.MetricDefinitionId)
            .NotEmpty().WithMessage("Metric definition ID is required.");

        RuleFor(x => x.ContributionType)
            .NotEmpty().WithMessage("Contribution type is required.")
            .Must(BeValidContributionType)
            .WithMessage("Invalid contribution type. Valid types: BooleanAs1, FixedValue, UseEnteredValue.");

        When(x => x.ContributionType == "FixedValue", () =>
        {
            RuleFor(x => x.FixedValue)
                .NotNull().WithMessage("Fixed value is required when contribution type is FixedValue.");
        });
    }

    private static bool BeValidContributionType(string type) => Enum.TryParse<HabitContributionType>(type, out _);
}

public sealed class CreateHabitVariantInputValidator : AbstractValidator<CreateHabitVariantInput>
{
    public CreateHabitVariantInputValidator()
    {
        RuleFor(x => x.Mode)
            .NotEmpty().WithMessage("Variant mode is required.")
            .Must(BeValidHabitMode).WithMessage("Invalid variant mode. Valid modes: Full, Maintenance, Minimum.");

        RuleFor(x => x.Label)
            .NotEmpty().WithMessage("Variant label is required.")
            .MaximumLength(100).WithMessage("Variant label cannot exceed 100 characters.");

        RuleFor(x => x.EstimatedMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Estimated minutes cannot be negative.");

        RuleFor(x => x.EnergyCost)
            .InclusiveBetween(1, 5).WithMessage("Energy cost must be between 1 and 5.");
    }

    private static bool BeValidHabitMode(string mode) => Enum.TryParse<HabitMode>(mode, out _);
}

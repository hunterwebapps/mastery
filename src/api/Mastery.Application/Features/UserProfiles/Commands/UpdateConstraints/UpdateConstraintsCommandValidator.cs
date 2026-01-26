using FluentValidation;

namespace Mastery.Application.Features.UserProfiles.Commands.UpdateConstraints;

public sealed class UpdateConstraintsCommandValidator : AbstractValidator<UpdateConstraintsCommand>
{
    public UpdateConstraintsCommandValidator()
    {
        RuleFor(x => x.Constraints)
            .NotNull().WithMessage("Constraints are required.");

        When(x => x.Constraints != null, () =>
        {
            RuleFor(x => x.Constraints.MaxPlannedMinutesWeekday)
                .InclusiveBetween(0, 1440)
                .WithMessage("Max planned minutes per weekday must be between 0 and 1440 (24 hours).");

            RuleFor(x => x.Constraints.MaxPlannedMinutesWeekend)
                .InclusiveBetween(0, 1440)
                .WithMessage("Max planned minutes per weekend day must be between 0 and 1440 (24 hours).");

            RuleFor(x => x.Constraints.HealthNotes)
                .MaximumLength(1000).WithMessage("Health notes cannot exceed 1000 characters.")
                .When(x => x.Constraints.HealthNotes != null);

            RuleForEach(x => x.Constraints.ContentBoundaries)
                .MaximumLength(100).WithMessage("Each content boundary cannot exceed 100 characters.");

            RuleForEach(x => x.Constraints.BlockedTimeWindows)
                .ChildRules(window =>
                {
                    window.RuleFor(w => w.Label)
                        .MaximumLength(100).WithMessage("Blocked window label cannot exceed 100 characters.")
                        .When(w => w.Label != null);

                    window.RuleFor(w => w.TimeWindow)
                        .NotNull().WithMessage("Time window is required for blocked windows.");

                    window.RuleFor(w => w.TimeWindow.Start)
                        .LessThan(w => w.TimeWindow.End)
                        .WithMessage("Start time must be before end time.")
                        .When(w => w.TimeWindow != null);

                    window.RuleFor(w => w.ApplicableDays)
                        .NotEmpty().WithMessage("At least one applicable day is required.");
                });

            RuleForEach(x => x.Constraints.NoNotificationsWindows)
                .ChildRules(window =>
                {
                    window.RuleFor(w => w.Start)
                        .LessThan(w => w.End)
                        .WithMessage("Start time must be before end time.");
                });
        });
    }
}

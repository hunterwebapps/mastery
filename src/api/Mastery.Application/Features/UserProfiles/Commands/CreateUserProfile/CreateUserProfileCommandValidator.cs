using FluentValidation;

namespace Mastery.Application.Features.UserProfiles.Commands.CreateUserProfile;

public sealed class CreateUserProfileCommandValidator : AbstractValidator<CreateUserProfileCommand>
{
    public CreateUserProfileCommandValidator()
    {
        RuleFor(x => x.Timezone)
            .NotEmpty().WithMessage("Timezone is required.")
            .Must(BeValidTimezone).WithMessage("Invalid timezone. Please use a valid IANA timezone ID.");

        RuleFor(x => x.Locale)
            .NotEmpty().WithMessage("Locale is required.")
            .Matches(@"^[a-z]{2}(-[A-Z]{2})?$").WithMessage("Invalid locale format. Expected: 'en' or 'en-US'.");

        RuleFor(x => x.Values)
            .NotEmpty().WithMessage("At least one value is required.");

        RuleForEach(x => x.Values)
            .ChildRules(value =>
            {
                value.RuleFor(v => v.Label)
                    .NotEmpty().WithMessage("Value label is required.")
                    .MaximumLength(100).WithMessage("Value label cannot exceed 100 characters.");

                value.RuleFor(v => v.Rank)
                    .GreaterThan(0).WithMessage("Value rank must be greater than 0.");
            });

        RuleFor(x => x.Roles)
            .NotEmpty().WithMessage("At least one role is required.");

        RuleForEach(x => x.Roles)
            .ChildRules(role =>
            {
                role.RuleFor(r => r.Label)
                    .NotEmpty().WithMessage("Role label is required.")
                    .MaximumLength(100).WithMessage("Role label cannot exceed 100 characters.");

                role.RuleFor(r => r.SeasonPriority)
                    .InclusiveBetween(1, 5).WithMessage("Season priority must be between 1 and 5.");

                role.RuleFor(r => r.MinWeeklyMinutes)
                    .GreaterThanOrEqualTo(0).WithMessage("Minimum weekly minutes cannot be negative.");

                role.RuleFor(r => r.TargetWeeklyMinutes)
                    .GreaterThanOrEqualTo(r => r.MinWeeklyMinutes)
                    .WithMessage("Target weekly minutes must be at least the minimum.");

                role.RuleFor(r => r.Status)
                    .Must(s => s == "Active" || s == "Inactive")
                    .WithMessage("Status must be 'Active' or 'Inactive'.");
            });

        When(x => x.Preferences != null, () =>
        {
            RuleFor(x => x.Preferences!.CoachingStyle)
                .Must(s => s == "Direct" || s == "Encouraging" || s == "Analytical")
                .WithMessage("Coaching style must be 'Direct', 'Encouraging', or 'Analytical'.");

            RuleFor(x => x.Preferences!.ExplanationVerbosity)
                .Must(s => s == "Minimal" || s == "Medium" || s == "Detailed")
                .WithMessage("Explanation verbosity must be 'Minimal', 'Medium', or 'Detailed'.");

            RuleFor(x => x.Preferences!.NudgeLevel)
                .Must(s => s == "Off" || s == "Low" || s == "Medium" || s == "High")
                .WithMessage("Nudge level must be 'Off', 'Low', 'Medium', or 'High'.");
        });

        When(x => x.Constraints != null, () =>
        {
            RuleFor(x => x.Constraints!.MaxPlannedMinutesWeekday)
                .InclusiveBetween(0, 1440)
                .WithMessage("Max planned minutes per weekday must be between 0 and 1440 (24 hours).");

            RuleFor(x => x.Constraints!.MaxPlannedMinutesWeekend)
                .InclusiveBetween(0, 1440)
                .WithMessage("Max planned minutes per weekend day must be between 0 and 1440 (24 hours).");
        });
    }

    private static bool BeValidTimezone(string timezone)
    {
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timezone);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

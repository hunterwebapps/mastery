using FluentValidation;

namespace Mastery.Application.Features.UserProfiles.Commands.UpdatePreferences;

public sealed class UpdatePreferencesCommandValidator : AbstractValidator<UpdatePreferencesCommand>
{
    public UpdatePreferencesCommandValidator()
    {
        RuleFor(x => x.Preferences)
            .NotNull().WithMessage("Preferences are required.");

        When(x => x.Preferences != null, () =>
        {
            RuleFor(x => x.Preferences.CoachingStyle)
                .NotEmpty().WithMessage("Coaching style is required.")
                .Must(BeValidCoachingStyle).WithMessage("Coaching style must be 'Direct', 'Encouraging', or 'Analytical'.");

            RuleFor(x => x.Preferences.ExplanationVerbosity)
                .NotEmpty().WithMessage("Explanation verbosity is required.")
                .Must(BeValidVerbosity).WithMessage("Explanation verbosity must be 'Minimal', 'Medium', or 'Detailed'.");

            RuleFor(x => x.Preferences.NudgeLevel)
                .NotEmpty().WithMessage("Nudge level is required.")
                .Must(BeValidNudgeLevel).WithMessage("Nudge level must be 'Off', 'Low', 'Medium', or 'High'.");

            RuleFor(x => x.Preferences.MorningCheckInTime)
                .LessThan(x => x.Preferences.EveningCheckInTime)
                .WithMessage("Morning check-in time must be before evening check-in time.");

            RuleFor(x => x.Preferences.PlanningDefaults)
                .NotNull().WithMessage("Planning defaults are required.");

            When(x => x.Preferences.PlanningDefaults != null, () =>
            {
                RuleFor(x => x.Preferences.PlanningDefaults.DefaultTaskDurationMinutes)
                    .InclusiveBetween(1, 480).WithMessage("Default task duration must be between 1 and 480 minutes.");

                RuleFor(x => x.Preferences.PlanningDefaults.BufferBetweenTasksMinutes)
                    .InclusiveBetween(0, 60).WithMessage("Buffer between tasks must be between 0 and 60 minutes.");
            });

            RuleFor(x => x.Preferences.Privacy)
                .NotNull().WithMessage("Privacy settings are required.");

            RuleForEach(x => x.Preferences.NotificationChannels)
                .Must(BeValidNotificationChannel)
                .WithMessage("Invalid notification channel. Must be 'Push', 'Email', or 'SMS'.");
        });
    }

    private static bool BeValidCoachingStyle(string style)
        => style is "Direct" or "Encouraging" or "Analytical";

    private static bool BeValidVerbosity(string verbosity)
        => verbosity is "Minimal" or "Medium" or "Detailed";

    private static bool BeValidNudgeLevel(string level)
        => level is "Off" or "Low" or "Medium" or "High";

    private static bool BeValidNotificationChannel(string channel)
        => channel is "Push" or "Email" or "SMS";
}

using Mastery.Domain.ValueObjects;

namespace Mastery.Domain.Entities.UserProfile;

/// <summary>
/// User preferences for how the system interacts with them (owned entity in UserProfile).
/// </summary>
public sealed class Preferences
{
    /// <summary>
    /// Preferred coaching communication style.
    /// </summary>
    public CoachingStyle CoachingStyle { get; set; } = CoachingStyle.Encouraging;

    /// <summary>
    /// How detailed explanations should be.
    /// </summary>
    public VerbosityLevel ExplanationVerbosity { get; set; } = VerbosityLevel.Medium;

    /// <summary>
    /// How frequently and intensely to send reminders.
    /// </summary>
    public NudgeLevel NudgeLevel { get; set; } = NudgeLevel.Medium;

    /// <summary>
    /// Enabled notification channels.
    /// </summary>
    public List<NotificationChannel> NotificationChannels { get; set; } = [NotificationChannel.Push];

    /// <summary>
    /// Scheduled times for morning and evening check-ins.
    /// </summary>
    public CheckInSchedule CheckInSchedule { get; set; } = CheckInSchedule.Default;

    /// <summary>
    /// Default settings for planning operations.
    /// </summary>
    public PlanningDefaults PlanningDefaults { get; set; } = new();

    /// <summary>
    /// Privacy-related settings.
    /// </summary>
    public PrivacySettings Privacy { get; set; } = new();
}

public enum CoachingStyle
{
    /// <summary>
    /// Straight to the point, minimal fluff.
    /// </summary>
    Direct,

    /// <summary>
    /// Supportive and motivating tone.
    /// </summary>
    Encouraging,

    /// <summary>
    /// Data-driven with detailed reasoning.
    /// </summary>
    Analytical
}

public enum VerbosityLevel
{
    /// <summary>
    /// Just the essentials.
    /// </summary>
    Minimal,

    /// <summary>
    /// Balanced detail.
    /// </summary>
    Medium,

    /// <summary>
    /// Full context and explanation.
    /// </summary>
    Detailed
}

public enum NudgeLevel
{
    /// <summary>
    /// No proactive nudges.
    /// </summary>
    Off,

    /// <summary>
    /// Occasional reminders for important items.
    /// </summary>
    Low,

    /// <summary>
    /// Regular check-ins and reminders.
    /// </summary>
    Medium,

    /// <summary>
    /// Frequent prompts to stay on track.
    /// </summary>
    High
}

public enum NotificationChannel
{
    Push,
    Email,
    Sms
}

/// <summary>
/// Default settings for the planning engine.
/// </summary>
public sealed class PlanningDefaults
{
    /// <summary>
    /// Default duration for tasks without an estimate.
    /// </summary>
    public int DefaultTaskDurationMinutes { get; set; } = 30;

    /// <summary>
    /// Whether to automatically schedule habits in the daily plan.
    /// </summary>
    public bool AutoScheduleHabits { get; set; } = true;

    /// <summary>
    /// Buffer time between scheduled tasks.
    /// </summary>
    public int BufferBetweenTasksMinutes { get; set; } = 5;
}

/// <summary>
/// Privacy-related preferences.
/// </summary>
public sealed class PrivacySettings
{
    /// <summary>
    /// Whether to share progress with an accountability coach.
    /// </summary>
    public bool ShareProgressWithCoach { get; set; } = false;

    /// <summary>
    /// Whether to allow anonymous usage analytics.
    /// </summary>
    public bool AllowAnonymousAnalytics { get; set; } = true;
}

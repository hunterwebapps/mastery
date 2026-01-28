namespace Mastery.Domain.Enums;

/// <summary>
/// Defines the scheduling pattern for a habit.
/// </summary>
public enum ScheduleType
{
    /// <summary>
    /// Habit is due every day.
    /// </summary>
    Daily,

    /// <summary>
    /// Habit is due on specific days of the week.
    /// </summary>
    DaysOfWeek,

    /// <summary>
    /// Habit is due a certain number of times per week (flexible days).
    /// </summary>
    WeeklyFrequency,

    /// <summary>
    /// Habit is due every N days (e.g., every 3 days).
    /// </summary>
    Interval
}

namespace Mastery.Domain.Enums;

/// <summary>
/// Represents the lifecycle status of a habit.
/// </summary>
public enum HabitStatus
{
    /// <summary>
    /// Habit is actively being tracked.
    /// </summary>
    Active,

    /// <summary>
    /// Habit is temporarily paused (not scheduled, but not archived).
    /// </summary>
    Paused,

    /// <summary>
    /// Habit is archived and no longer visible in active views.
    /// </summary>
    Archived
}

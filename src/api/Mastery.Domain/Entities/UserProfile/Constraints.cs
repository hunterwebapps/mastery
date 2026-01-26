using Mastery.Domain.ValueObjects;

namespace Mastery.Domain.Entities.UserProfile;

/// <summary>
/// Hard constraints for the planning engine (owned entity in UserProfile).
/// These guardrails must be respected by deterministic scheduling.
/// </summary>
public sealed class Constraints
{
    /// <summary>
    /// Maximum planned minutes per weekday (default: 8 hours).
    /// </summary>
    public int MaxPlannedMinutesWeekday { get; set; } = 480;

    /// <summary>
    /// Maximum planned minutes per weekend day (default: 4 hours).
    /// </summary>
    public int MaxPlannedMinutesWeekend { get; set; } = 240;

    /// <summary>
    /// Time windows when no tasks should be scheduled.
    /// Example: family dinner time, exercise windows.
    /// </summary>
    public List<BlockedWindow> BlockedTimeWindows { get; set; } = [];

    /// <summary>
    /// Time windows when notifications should not be sent.
    /// </summary>
    public List<TimeWindow> NoNotificationsWindows { get; set; } = [];

    /// <summary>
    /// User-controlled health or accessibility notes.
    /// Non-medical, just context for the system.
    /// </summary>
    public string? HealthNotes { get; set; }

    /// <summary>
    /// Content boundaries for coaching language.
    /// Example: "no dieting advice", "avoid shame language".
    /// </summary>
    public List<string> ContentBoundaries { get; set; } = [];
}

/// <summary>
/// A blocked time window with an optional label and day-of-week constraints.
/// </summary>
public sealed class BlockedWindow
{
    /// <summary>
    /// Human-readable label for this blocked window.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// The time window that is blocked.
    /// </summary>
    public required TimeWindow TimeWindow { get; set; }

    /// <summary>
    /// Days of the week this block applies to.
    /// Empty means all days.
    /// </summary>
    public List<DayOfWeek> ApplicableDays { get; set; } = [];

    /// <summary>
    /// Checks if this blocked window applies to a given day and time.
    /// </summary>
    public bool AppliesTo(DayOfWeek day, TimeOnly time)
    {
        var dayMatches = ApplicableDays.Count == 0 || ApplicableDays.Contains(day);
        return dayMatches && TimeWindow.Contains(time);
    }
}

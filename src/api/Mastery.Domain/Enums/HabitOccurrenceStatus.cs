namespace Mastery.Domain.Enums;

/// <summary>
/// Represents the status of a single habit occurrence (a scheduled instance).
/// </summary>
public enum HabitOccurrenceStatus
{
    /// <summary>
    /// Occurrence is scheduled but not yet completed or missed.
    /// </summary>
    Pending,

    /// <summary>
    /// User completed the habit for this occurrence.
    /// </summary>
    Completed,

    /// <summary>
    /// Occurrence was missed (past due without completion).
    /// </summary>
    Missed,

    /// <summary>
    /// User explicitly skipped this occurrence.
    /// </summary>
    Skipped,

    /// <summary>
    /// Occurrence was rescheduled to a different date.
    /// </summary>
    Rescheduled
}

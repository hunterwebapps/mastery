namespace Mastery.Domain.Enums;

/// <summary>
/// Represents the lifecycle status of a check-in.
/// </summary>
public enum CheckInStatus
{
    /// <summary>
    /// Check-in started but not yet completed (reserved for future save-progress).
    /// </summary>
    Draft,

    /// <summary>
    /// Check-in fully completed.
    /// </summary>
    Completed,

    /// <summary>
    /// User explicitly skipped this check-in.
    /// </summary>
    Skipped
}

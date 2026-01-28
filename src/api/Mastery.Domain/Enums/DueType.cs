namespace Mastery.Domain.Enums;

/// <summary>
/// Distinguishes between soft and hard due dates for psychological safety.
/// </summary>
public enum DueType
{
    /// <summary>
    /// Gentle guidance. Shows as "should do by" but doesn't generate miss failure events.
    /// Used for ordering and gentle prioritization.
    /// </summary>
    Soft,

    /// <summary>
    /// Hard commitment. Generates explicit overdue/risk signals.
    /// Can trigger nudges and feeds drift detection.
    /// </summary>
    Hard
}

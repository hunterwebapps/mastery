namespace Mastery.Domain.Enums;

/// <summary>
/// Type of processing window for signal scheduling.
/// </summary>
public enum ProcessingWindowType
{
    /// <summary>
    /// Process immediately (for urgent signals).
    /// </summary>
    Immediate,

    /// <summary>
    /// Process during user's morning window (typically 6-9 AM local time).
    /// </summary>
    MorningWindow,

    /// <summary>
    /// Process during user's evening window (typically 8-10 PM local time).
    /// </summary>
    EveningWindow,

    /// <summary>
    /// Process during user's weekly review window (typically Sunday evening).
    /// </summary>
    WeeklyReview,

    /// <summary>
    /// Process in next batch run (for standard and low priority signals).
    /// </summary>
    BatchWindow,
}

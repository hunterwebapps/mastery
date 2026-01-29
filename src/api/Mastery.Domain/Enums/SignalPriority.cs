namespace Mastery.Domain.Enums;

/// <summary>
/// Priority level for signal processing. Determines how quickly a signal should be processed.
/// </summary>
public enum SignalPriority
{
    /// <summary>
    /// Process within 5 minutes. Used for capacity overload, imminent deadline, streak breaking.
    /// </summary>
    Urgent = 0,

    /// <summary>
    /// Process at user's natural window (morning/evening/weekly). Used for check-ins, reviews.
    /// </summary>
    WindowAligned = 1,

    /// <summary>
    /// Process within 4-6 hours in batch. Used for habit/task completions, metric recordings.
    /// </summary>
    Standard = 2,

    /// <summary>
    /// Process within 24 hours in background. Used for metadata updates, preference changes.
    /// </summary>
    Low = 3
}

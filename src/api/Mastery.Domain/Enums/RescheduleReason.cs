namespace Mastery.Domain.Enums;

/// <summary>
/// Categorized reasons for rescheduling a task.
/// Powers friction analysis in the diagnostic engine.
/// </summary>
public enum RescheduleReason
{
    /// <summary>
    /// Not enough time available.
    /// </summary>
    NoTime,

    /// <summary>
    /// Energy too low to execute.
    /// </summary>
    TooTired,

    /// <summary>
    /// Blocked by external factors or dependencies.
    /// </summary>
    Blocked,

    /// <summary>
    /// Simply forgot about it.
    /// </summary>
    Forgot,

    /// <summary>
    /// Task scope was too large for the available time.
    /// </summary>
    ScopeTooBig,

    /// <summary>
    /// Waiting on someone else before proceeding.
    /// </summary>
    WaitingOnSomeone,

    /// <summary>
    /// Other uncategorized reason.
    /// </summary>
    Other
}

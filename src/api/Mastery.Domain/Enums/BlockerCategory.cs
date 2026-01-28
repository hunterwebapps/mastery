namespace Mastery.Domain.Enums;

/// <summary>
/// Categorizes the biggest blocker of the day for friction analysis.
/// Aligned with MissReason taxonomy for diagnostic consistency.
/// </summary>
public enum BlockerCategory
{
    /// <summary>
    /// Too tired or low energy.
    /// </summary>
    TooTired,

    /// <summary>
    /// No time available.
    /// </summary>
    NoTime,

    /// <summary>
    /// Simply forgot or lost track.
    /// </summary>
    Forgot,

    /// <summary>
    /// Environment was not conducive.
    /// </summary>
    Environment,

    /// <summary>
    /// Scheduling conflict with other commitment.
    /// </summary>
    Conflict,

    /// <summary>
    /// Illness or health issue.
    /// </summary>
    Sickness,

    /// <summary>
    /// Other reason (captured in blocker note).
    /// </summary>
    Other
}

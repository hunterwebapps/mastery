namespace Mastery.Domain.Enums;

/// <summary>
/// Categorizes why a habit was missed for friction analysis.
/// </summary>
public enum MissReason
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
    /// Simply forgot.
    /// </summary>
    Forgot,

    /// <summary>
    /// Environment was not conducive (e.g., traveling, wrong location).
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
    /// Other reason (captured in notes).
    /// </summary>
    Other
}

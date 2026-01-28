namespace Mastery.Domain.Enums;

/// <summary>
/// Represents the type of target comparison for a metric.
/// </summary>
public enum TargetType
{
    /// <summary>
    /// Value must be at least the target (e.g., >= 30 min/day).
    /// </summary>
    AtLeast,

    /// <summary>
    /// Value must be at most the target (e.g., <= 2 drinks/week).
    /// </summary>
    AtMost,

    /// <summary>
    /// Value must be between min and max (e.g., sleep 7-9 hours).
    /// </summary>
    Between,

    /// <summary>
    /// Value must equal exactly (rare; for fixed deliverables).
    /// </summary>
    Exactly
}

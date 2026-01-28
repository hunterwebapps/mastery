namespace Mastery.Domain.Enums;

/// <summary>
/// Defines how a habit completion contributes to its bound metric.
/// </summary>
public enum HabitContributionType
{
    /// <summary>
    /// Each completion adds 1 to the metric (boolean tracking).
    /// </summary>
    BooleanAs1,

    /// <summary>
    /// Each completion adds a fixed value defined on the binding.
    /// </summary>
    FixedValue,

    /// <summary>
    /// User enters the actual value at completion time.
    /// </summary>
    UseEnteredValue
}

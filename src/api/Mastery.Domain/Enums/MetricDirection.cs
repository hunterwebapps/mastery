namespace Mastery.Domain.Enums;

/// <summary>
/// Represents the desired direction of change for a metric.
/// </summary>
public enum MetricDirection
{
    /// <summary>
    /// Higher values are better (e.g., revenue, workout minutes).
    /// </summary>
    Increase,

    /// <summary>
    /// Lower values are better (e.g., weight loss, expenses).
    /// </summary>
    Decrease,

    /// <summary>
    /// Value should stay within a range (e.g., sleep hours).
    /// </summary>
    Maintain
}

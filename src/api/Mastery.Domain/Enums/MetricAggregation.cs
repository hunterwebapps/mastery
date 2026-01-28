namespace Mastery.Domain.Enums;

/// <summary>
/// Represents how metric observations are aggregated over an evaluation window.
/// </summary>
public enum MetricAggregation
{
    /// <summary>
    /// Sum all values in the window (e.g., total workout minutes).
    /// </summary>
    Sum,

    /// <summary>
    /// Average of all values in the window (e.g., average sleep hours).
    /// </summary>
    Average,

    /// <summary>
    /// Maximum value in the window.
    /// </summary>
    Max,

    /// <summary>
    /// Minimum value in the window.
    /// </summary>
    Min,

    /// <summary>
    /// Count of observations in the window (e.g., number of gym visits).
    /// </summary>
    Count,

    /// <summary>
    /// Most recent value in the window (e.g., current weight).
    /// </summary>
    Latest
}

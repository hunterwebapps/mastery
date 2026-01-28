namespace Mastery.Domain.Enums;

/// <summary>
/// Represents the data type of a metric's values.
/// </summary>
public enum MetricDataType
{
    /// <summary>
    /// Numeric value (e.g., weight, revenue).
    /// </summary>
    Number,

    /// <summary>
    /// Boolean yes/no (e.g., "Did I meditate?").
    /// </summary>
    Boolean,

    /// <summary>
    /// Time duration in minutes (e.g., deep work time).
    /// </summary>
    Duration,

    /// <summary>
    /// Percentage value 0-100 (e.g., completion rate).
    /// </summary>
    Percentage,

    /// <summary>
    /// Count of occurrences (e.g., gym sessions).
    /// </summary>
    Count,

    /// <summary>
    /// Rating scale 1-5 (e.g., energy level).
    /// </summary>
    Rating
}

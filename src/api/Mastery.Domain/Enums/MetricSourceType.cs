namespace Mastery.Domain.Enums;

/// <summary>
/// Represents the source of a metric observation.
/// </summary>
public enum MetricSourceType
{
    /// <summary>
    /// User manually entered the value.
    /// </summary>
    Manual,

    /// <summary>
    /// Derived from habit completions.
    /// </summary>
    Habit,

    /// <summary>
    /// Derived from task completions.
    /// </summary>
    Task,

    /// <summary>
    /// Derived from check-in data.
    /// </summary>
    CheckIn,

    /// <summary>
    /// Imported from external integration (calendar, health app, etc.).
    /// </summary>
    Integration,

    /// <summary>
    /// Computed from other metrics or system calculations.
    /// </summary>
    Computed
}

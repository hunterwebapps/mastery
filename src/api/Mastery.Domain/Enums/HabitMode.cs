namespace Mastery.Domain.Enums;

/// <summary>
/// Represents the execution mode/intensity level for a habit.
/// Enables graceful degradation during low-capacity periods.
/// </summary>
public enum HabitMode
{
    /// <summary>
    /// Full version of the habit (default, complete execution).
    /// </summary>
    Full,

    /// <summary>
    /// Reduced version for maintaining the streak during moderate capacity.
    /// </summary>
    Maintenance,

    /// <summary>
    /// Bare minimum version to keep the chain alive during low capacity.
    /// </summary>
    Minimum
}

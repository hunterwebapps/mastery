namespace Mastery.Domain.Enums;

/// <summary>
/// Represents what kind of entity the user selected as their Top 1 priority for the day.
/// </summary>
public enum Top1Type
{
    /// <summary>
    /// A task from the task system.
    /// </summary>
    Task,

    /// <summary>
    /// A habit from the habit system.
    /// </summary>
    Habit,

    /// <summary>
    /// A project next action.
    /// </summary>
    Project,

    /// <summary>
    /// Free-text entry (creates an inbox item optionally).
    /// </summary>
    FreeText
}

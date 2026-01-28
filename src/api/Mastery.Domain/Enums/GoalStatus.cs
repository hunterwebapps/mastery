namespace Mastery.Domain.Enums;

/// <summary>
/// Represents the lifecycle status of a goal.
/// </summary>
public enum GoalStatus
{
    /// <summary>
    /// Goal is being defined but not yet active.
    /// </summary>
    Draft,

    /// <summary>
    /// Goal is actively being worked on.
    /// </summary>
    Active,

    /// <summary>
    /// Goal is temporarily paused (keeps history, excluded from planning).
    /// </summary>
    Paused,

    /// <summary>
    /// Goal has been achieved or intentionally ended.
    /// </summary>
    Completed,

    /// <summary>
    /// Goal is hidden from default UI but history remains.
    /// </summary>
    Archived
}

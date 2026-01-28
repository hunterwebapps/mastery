namespace Mastery.Domain.Enums;

/// <summary>
/// Represents the status of a project milestone.
/// </summary>
public enum MilestoneStatus
{
    /// <summary>
    /// Milestone not yet started.
    /// </summary>
    NotStarted,

    /// <summary>
    /// Milestone is being worked on.
    /// </summary>
    InProgress,

    /// <summary>
    /// Milestone completed.
    /// </summary>
    Completed
}

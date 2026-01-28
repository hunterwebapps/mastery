namespace Mastery.Domain.Enums;

/// <summary>
/// Represents the lifecycle status of a task.
/// Tasks are the primary "actuators" in the control loop - converting intention into execution.
/// </summary>
public enum TaskStatus
{
    /// <summary>
    /// Captured but not triaged. Lives in inbox.
    /// </summary>
    Inbox,

    /// <summary>
    /// Actionable and eligible for Next Best Action ranking.
    /// </summary>
    Ready,

    /// <summary>
    /// Assigned to a specific date (and optionally time window).
    /// </summary>
    Scheduled,

    /// <summary>
    /// Currently being worked on.
    /// </summary>
    InProgress,

    /// <summary>
    /// Successfully completed. Terminal success state.
    /// </summary>
    Completed,

    /// <summary>
    /// Intentionally not doing. Important diagnostic signal for plan realism.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Soft-deleted / hidden from active views.
    /// </summary>
    Archived
}

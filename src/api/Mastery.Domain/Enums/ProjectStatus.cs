namespace Mastery.Domain.Enums;

/// <summary>
/// Represents the lifecycle status of a project.
/// Projects are execution containers for achieving goals.
/// </summary>
public enum ProjectStatus
{
    /// <summary>
    /// Being defined, not yet active.
    /// </summary>
    Draft,

    /// <summary>
    /// Actively working on. Should have at least one Ready task.
    /// </summary>
    Active,

    /// <summary>
    /// Temporarily on hold.
    /// </summary>
    Paused,

    /// <summary>
    /// Successfully completed.
    /// </summary>
    Completed,

    /// <summary>
    /// Soft-deleted / hidden from active views.
    /// </summary>
    Archived
}

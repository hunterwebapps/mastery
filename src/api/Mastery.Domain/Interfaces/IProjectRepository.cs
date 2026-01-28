using Mastery.Domain.Entities.Project;
using Mastery.Domain.Enums;
using TaskStatus = Mastery.Domain.Enums.TaskStatus;

namespace Mastery.Domain.Interfaces;

public interface IProjectRepository : IRepository<Project>
{
    /// <summary>
    /// Gets all projects for a user.
    /// </summary>
    Task<IReadOnlyList<Project>> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets projects for a user filtered by status.
    /// </summary>
    Task<IReadOnlyList<Project>> GetByStatusAsync(
        string userId,
        ProjectStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets projects for a specific goal.
    /// </summary>
    Task<IReadOnlyList<Project>> GetByGoalIdAsync(
        Guid goalId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active projects that have no next action set (stuck projects).
    /// </summary>
    Task<IReadOnlyList<Project>> GetActiveWithoutNextActionAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a project with all its milestones eagerly loaded.
    /// </summary>
    Task<Project?> GetByIdWithMilestonesAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a project with milestones and task counts by status.
    /// Useful for project detail view.
    /// </summary>
    Task<Project?> GetByIdWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a project exists and belongs to the specified user.
    /// </summary>
    Task<bool> ExistsByIdAndUserIdAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active projects for a user with their next task title for list views.
    /// </summary>
    Task<IReadOnlyList<Project>> GetActiveWithSummaryAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets task counts by status for a project.
    /// </summary>
    Task<Dictionary<TaskStatus, int>> GetTaskCountsByStatusAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets projects nearing their target end date.
    /// </summary>
    Task<IReadOnlyList<Project>> GetNearingDeadlineAsync(
        string userId,
        DateOnly referenceDate,
        int daysAhead,
        CancellationToken cancellationToken = default);
}

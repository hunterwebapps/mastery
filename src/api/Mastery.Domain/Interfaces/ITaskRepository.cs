using Mastery.Domain.Enums;
using TaskStatus = Mastery.Domain.Enums.TaskStatus;

namespace Mastery.Domain.Interfaces;

public interface ITaskRepository : IRepository<Entities.Task.Task>
{
    /// <summary>
    /// Gets all tasks for a user, ordered by display order.
    /// </summary>
    Task<IReadOnlyList<Entities.Task.Task>> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tasks for a user filtered by status.
    /// </summary>
    Task<IReadOnlyList<Entities.Task.Task>> GetByStatusAsync(
        string userId,
        TaskStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tasks for a specific project.
    /// </summary>
    Task<IReadOnlyList<Entities.Task.Task>> GetByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tasks for a specific goal.
    /// </summary>
    Task<IReadOnlyList<Entities.Task.Task>> GetByGoalIdAsync(
        Guid goalId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tasks scheduled for today + due today + overdue hard due dates.
    /// Optimized query for the daily loop.
    /// </summary>
    Task<IReadOnlyList<Entities.Task.Task>> GetTodayTasksAsync(
        string userId,
        DateOnly today,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tasks in Inbox status.
    /// </summary>
    Task<IReadOnlyList<Entities.Task.Task>> GetInboxTasksAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tasks with overdue hard due dates.
    /// </summary>
    Task<IReadOnlyList<Entities.Task.Task>> GetOverdueTasksAsync(
        string userId,
        DateOnly today,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tasks in Ready status (actionable but not scheduled).
    /// </summary>
    Task<IReadOnlyList<Entities.Task.Task>> GetReadyTasksAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a task with all its details eagerly loaded (metric bindings).
    /// </summary>
    Task<Entities.Task.Task?> GetByIdWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unresolved dependency task IDs for a task.
    /// Returns IDs of dependency tasks that are not completed or cancelled.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetUnresolvedDependenciesAsync(
        Guid taskId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a task is blocked by any unresolved dependencies.
    /// </summary>
    Task<bool> IsBlockedAsync(
        Guid taskId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a task exists and belongs to the specified user.
    /// </summary>
    Task<bool> ExistsByIdAndUserIdAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the maximum display order for a user's tasks.
    /// </summary>
    Task<int> GetMaxDisplayOrderAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tasks eligible for NBA (Next Best Action) ranking.
    /// Returns Ready and Scheduled tasks that are not blocked.
    /// </summary>
    Task<IReadOnlyList<Entities.Task.Task>> GetNBAEligibleTasksAsync(
        string userId,
        DateOnly today,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tasks with specified context tags.
    /// </summary>
    Task<IReadOnlyList<Entities.Task.Task>> GetByContextTagAsync(
        string userId,
        ContextTag contextTag,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets completed tasks within a date range for reporting.
    /// </summary>
    Task<IReadOnlyList<Entities.Task.Task>> GetCompletedInRangeAsync(
        string userId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);
}

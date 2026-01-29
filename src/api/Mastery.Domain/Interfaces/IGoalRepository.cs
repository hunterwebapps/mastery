using Mastery.Domain.Entities.Goal;
using Mastery.Domain.Enums;

namespace Mastery.Domain.Interfaces;

public interface IGoalRepository : IRepository<Goal>
{
    /// <summary>
    /// Gets all goals for a user, ordered by priority and creation date.
    /// </summary>
    Task<IReadOnlyList<Goal>> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets goals for a user filtered by status.
    /// </summary>
    Task<IReadOnlyList<Goal>> GetByUserIdAndStatusAsync(
        string userId,
        GoalStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active goals for a user (includes Active and Paused).
    /// </summary>
    Task<IReadOnlyList<Goal>> GetActiveGoalsByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a goal with its metrics eagerly loaded.
    /// </summary>
    Task<Goal?> GetByIdWithMetricsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets goals associated with a specific season.
    /// </summary>
    Task<IReadOnlyList<Goal>> GetBySeasonIdAsync(
        Guid seasonId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a goal exists and belongs to the specified user.
    /// </summary>
    Task<bool> ExistsByIdAndUserIdAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a GoalMetric to be tracked by the DbContext.
    /// Use this when adding a metric to a goal's collection to ensure EF Core tracks it.
    /// </summary>
    Task AddGoalMetricAsync(
        GoalMetric metric,
        CancellationToken cancellationToken = default);
}

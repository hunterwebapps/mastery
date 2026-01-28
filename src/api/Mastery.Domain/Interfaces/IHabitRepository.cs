using Mastery.Domain.Entities.Habit;
using Mastery.Domain.Enums;

namespace Mastery.Domain.Interfaces;

public interface IHabitRepository : IRepository<Habit>
{
    /// <summary>
    /// Gets all habits for a user, ordered by display order.
    /// </summary>
    Task<IReadOnlyList<Habit>> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active habits for a user.
    /// </summary>
    Task<IReadOnlyList<Habit>> GetActiveByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets habits for a user filtered by status.
    /// </summary>
    Task<IReadOnlyList<Habit>> GetByStatusAsync(
        string userId,
        HabitStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a habit with all its details eagerly loaded (bindings, variants).
    /// </summary>
    Task<Habit?> GetByIdWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a habit with occurrences in a date range.
    /// </summary>
    Task<Habit?> GetByIdWithOccurrencesAsync(
        Guid id,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets habits that are due on a specific date.
    /// </summary>
    Task<IReadOnlyList<Habit>> GetDueOnDateAsync(
        string userId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a habit exists and belongs to the specified user.
    /// </summary>
    Task<bool> ExistsByIdAndUserIdAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets habits with their today occurrence for the Today view.
    /// Optimized query for the daily loop.
    /// </summary>
    Task<IReadOnlyList<Habit>> GetTodayHabitsAsync(
        string userId,
        DateOnly today,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current streak counts for all active habits of a user.
    /// </summary>
    Task<Dictionary<Guid, int>> GetStreaksAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets adherence rates for all active habits of a user over the specified number of days.
    /// </summary>
    Task<Dictionary<Guid, decimal>> GetAdherenceRatesAsync(
        string userId,
        int days,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the maximum display order for a user's habits.
    /// </summary>
    Task<int> GetMaxDisplayOrderAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the current streak for a specific habit.
    /// </summary>
    Task<int> CalculateStreakAsync(
        Guid habitId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new occurrence to the change tracker.
    /// Use this when adding occurrences to habits loaded with filtered includes.
    /// </summary>
    Task AddOccurrenceAsync(
        HabitOccurrence occurrence,
        CancellationToken cancellationToken = default);
}

using Mastery.Domain.Entities.CheckIn;
using Mastery.Domain.Enums;

namespace Mastery.Domain.Interfaces;

public interface ICheckInRepository : IRepository<CheckIn>
{
    /// <summary>
    /// Gets both morning and evening check-ins for a user on a specific date.
    /// </summary>
    Task<IReadOnlyList<CheckIn>> GetByUserIdAndDateAsync(
        string userId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific check-in by user, date, and type.
    /// </summary>
    Task<CheckIn?> GetByUserIdAndDateAndTypeAsync(
        string userId,
        DateOnly date,
        CheckInType type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets check-ins for a user in a date range, ordered by date descending.
    /// </summary>
    Task<IReadOnlyList<CheckIn>> GetByUserIdAndDateRangeAsync(
        string userId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets today's check-in state (both morning and evening) for the daily loop.
    /// </summary>
    Task<IReadOnlyList<CheckIn>> GetTodayStateAsync(
        string userId,
        DateOnly today,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a check-in already exists for the given user, date, and type.
    /// Used to enforce one morning + one evening per user per date.
    /// </summary>
    Task<bool> ExistsByUserIdAndDateAndTypeAsync(
        string userId,
        DateOnly date,
        CheckInType type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts consecutive days with at least one completed check-in, ending at the given date.
    /// Used for streak calculation.
    /// </summary>
    Task<int> CalculateStreakAsync(
        string userId,
        DateOnly upToDate,
        CancellationToken cancellationToken = default);
}

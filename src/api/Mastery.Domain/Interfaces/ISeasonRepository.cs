using Mastery.Domain.Entities;

namespace Mastery.Domain.Interfaces;

public interface ISeasonRepository : IRepository<Season>
{
    /// <summary>
    /// Gets all seasons for a user, ordered by start date (most recent first).
    /// </summary>
    Task<IReadOnlyList<Season>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent active (not ended) season for a user.
    /// </summary>
    Task<Season?> GetActiveSeasonForUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets seasons for a user within a date range.
    /// </summary>
    Task<IReadOnlyList<Season>> GetByUserIdAndDateRangeAsync(
        string userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
}

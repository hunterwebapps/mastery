using Mastery.Domain.Entities.Recommendation;

namespace Mastery.Domain.Interfaces;

public interface IRecommendationRunHistoryRepository : IRepository<RecommendationRunHistory>
{
    Task<RecommendationRunHistory?> GetLastCompletedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecommendationRunHistory>> GetRecentAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns distinct user IDs that have had entity changes (created or modified) since the given timestamp.
    /// Queries across Goals, Habits, Tasks, Projects, CheckIns, Experiments, MetricObservations, and HabitOccurrences.
    /// </summary>
    Task<IReadOnlyList<string>> GetUserIdsWithChangesSinceAsync(DateTime since, CancellationToken cancellationToken = default);
}

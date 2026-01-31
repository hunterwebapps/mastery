using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;

namespace Mastery.Domain.Interfaces;

public interface IRecommendationRepository : IRepository<Recommendation>
{
    Task<IReadOnlyList<Recommendation>> GetActiveByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Recommendation>> GetByUserIdAndContextAsync(
        string userId,
        RecommendationContext context,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Recommendation>> GetByUserIdAndStatusAsync(
        string userId,
        RecommendationStatus status,
        CancellationToken cancellationToken = default);

    Task<Recommendation?> GetByIdWithTraceAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Recommendation>> GetHistoryAsync(
        string userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    Task ExpirePendingBeforeAsync(
        string userId,
        DateTime cutoff,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByIdAndUserIdAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a pending recommendation already exists for a specific target entity and type.
    /// Used for deduplication to avoid creating multiple similar recommendations.
    /// </summary>
    Task<bool> ExistsPendingForTargetAsync(
        string userId,
        RecommendationType type,
        RecommendationTargetKind targetKind,
        Guid? targetEntityId,
        CancellationToken cancellationToken = default);
}

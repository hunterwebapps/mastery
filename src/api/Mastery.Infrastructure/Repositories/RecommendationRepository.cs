using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;
using Mastery.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mastery.Infrastructure.Repositories;

public class RecommendationRepository : BaseRepository<Recommendation>, IRecommendationRepository
{
    public RecommendationRepository(MasteryDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Recommendation>> GetActiveByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(r => r.Trace)
            .Where(r => r.UserId == userId &&
                        (r.Status == RecommendationStatus.Pending || r.Status == RecommendationStatus.Snoozed))
            .OrderByDescending(r => r.Score)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Recommendation>> GetByUserIdAndContextAsync(
        string userId,
        RecommendationContext context,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(r => r.Trace)
            .Where(r => r.UserId == userId && r.Context == context &&
                        (r.Status == RecommendationStatus.Pending || r.Status == RecommendationStatus.Snoozed))
            .OrderByDescending(r => r.Score)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Recommendation>> GetByUserIdAndStatusAsync(
        string userId,
        RecommendationStatus status,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(r => r.UserId == userId && r.Status == status)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Recommendation?> GetByIdWithTraceAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(r => r.Trace)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Recommendation>> GetHistoryAsync(
        string userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Where(r => r.UserId == userId);

        if (fromDate.HasValue)
            query = query.Where(r => r.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(r => r.CreatedAt <= toDate.Value);

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task ExpirePendingBeforeAsync(
        string userId,
        DateTime cutoff,
        CancellationToken cancellationToken = default)
    {
        var staleRecommendations = await DbSet
            .Where(r => r.UserId == userId &&
                        (r.Status == RecommendationStatus.Pending || r.Status == RecommendationStatus.Snoozed) &&
                        r.CreatedAt < cutoff)
            .ToListAsync(cancellationToken);

        foreach (var rec in staleRecommendations)
        {
            rec.MarkExpired();
        }
    }

    public async Task<bool> ExistsByIdAndUserIdAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(r => r.Id == id && r.UserId == userId, cancellationToken);
    }

    public async Task<bool> ExistsPendingForTargetAsync(
        string userId,
        RecommendationType type,
        RecommendationTargetKind targetKind,
        Guid? targetEntityId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(r => r.UserId == userId &&
                          r.Type == type &&
                          r.Target.Kind == targetKind &&
                          r.Target.EntityId == targetEntityId &&
                          (r.Status == RecommendationStatus.Pending || r.Status == RecommendationStatus.Snoozed),
                      cancellationToken);
    }
}

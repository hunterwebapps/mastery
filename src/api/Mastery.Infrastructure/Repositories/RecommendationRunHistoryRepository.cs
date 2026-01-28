using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Interfaces;
using Mastery.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mastery.Infrastructure.Repositories;

public class RecommendationRunHistoryRepository
    : BaseRepository<RecommendationRunHistory>, IRecommendationRunHistoryRepository
{
    private readonly MasteryDbContext _db;

    public RecommendationRunHistoryRepository(MasteryDbContext context) : base(context)
    {
        _db = context;
    }

    public async Task<RecommendationRunHistory?> GetLastCompletedAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(r => r.Status == "Completed" || r.Status == "Failed")
            .OrderByDescending(r => r.CompletedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecommendationRunHistory>> GetRecentAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .OrderByDescending(r => r.StartedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetUserIdsWithChangesSinceAsync(
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        // Query CreatedBy across all key auditable tables (AuditableEntity).
        // These have CreatedAt/CreatedBy/ModifiedAt/ModifiedBy set by MasteryDbContext.SaveChangesAsync.

        var goalUsers = _db.Goals
            .Where(e => e.CreatedAt > since || (e.ModifiedAt != null && e.ModifiedAt > since))
            .Select(e => e.CreatedBy!);

        var habitUsers = _db.Habits
            .Where(e => e.CreatedAt > since || (e.ModifiedAt != null && e.ModifiedAt > since))
            .Select(e => e.CreatedBy!);

        var taskUsers = _db.Tasks
            .Where(e => e.CreatedAt > since || (e.ModifiedAt != null && e.ModifiedAt > since))
            .Select(e => e.CreatedBy!);

        var projectUsers = _db.Projects
            .Where(e => e.CreatedAt > since || (e.ModifiedAt != null && e.ModifiedAt > since))
            .Select(e => e.CreatedBy!);

        var checkInUsers = _db.CheckIns
            .Where(e => e.CreatedAt > since || (e.ModifiedAt != null && e.ModifiedAt > since))
            .Select(e => e.CreatedBy!);

        var experimentUsers = _db.Experiments
            .Where(e => e.CreatedAt > since || (e.ModifiedAt != null && e.ModifiedAt > since))
            .Select(e => e.CreatedBy!);

        // MetricObservation and HabitOccurrence extend BaseEntity (not AuditableEntity),
        // so they have their own CreatedAt/UserId fields rather than CreatedBy/ModifiedAt.
        var metricObservationUsers = _db.MetricObservations
            .Where(e => e.CreatedAt > since)
            .Select(e => e.UserId);

        var allUserIds = await goalUsers
            .Union(habitUsers)
            .Union(taskUsers)
            .Union(projectUsers)
            .Union(checkInUsers)
            .Union(experimentUsers)
            .Union(metricObservationUsers)
            .Where(id => id != null && id != "system")
            .Distinct()
            .ToListAsync(cancellationToken);

        return allUserIds;
    }
}

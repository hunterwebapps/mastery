using Mastery.Domain.Entities.Goal;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;
using Mastery.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mastery.Infrastructure.Repositories;

public class GoalRepository : BaseRepository<Goal>, IGoalRepository
{
    public GoalRepository(MasteryDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Goal>> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(g => g.Metrics)
            .Where(g => g.UserId == userId)
            .OrderBy(g => g.Priority)
            .ThenByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Goal>> GetByUserIdAndStatusAsync(
        string userId,
        GoalStatus status,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(g => g.Metrics)
            .Where(g => g.UserId == userId && g.Status == status)
            .OrderBy(g => g.Priority)
            .ThenByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Goal>> GetActiveGoalsByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(g => g.Metrics)
            .Where(g => g.UserId == userId &&
                        (g.Status == GoalStatus.Active || g.Status == GoalStatus.Paused))
            .OrderBy(g => g.Priority)
            .ThenByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Goal?> GetByIdWithMetricsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(g => g.Metrics)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Goal>> GetBySeasonIdAsync(
        Guid seasonId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(g => g.Metrics)
            .Where(g => g.SeasonId == seasonId)
            .OrderBy(g => g.Priority)
            .ThenByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByIdAndUserIdAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(g => g.Id == id && g.UserId == userId, cancellationToken);
    }
}

using Mastery.Domain.Entities.Metrics;
using Mastery.Domain.Interfaces;
using Mastery.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mastery.Infrastructure.Repositories;

public class MetricDefinitionRepository : BaseRepository<MetricDefinition>, IMetricDefinitionRepository
{
    public MetricDefinitionRepository(MasteryDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<MetricDefinition>> GetByUserIdAsync(
        string userId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(m => m.UserId == userId);

        if (!includeArchived)
        {
            query = query.Where(m => !m.IsArchived);
        }

        return await query
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<MetricDefinition?> GetByUserIdAndNameAsync(
        string userId,
        string name,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Name == name, cancellationToken);
    }

    public async Task<bool> ExistsByIdAndUserIdAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(m => m.Id == id && m.UserId == userId, cancellationToken);
    }

    public async Task<bool> ExistsByUserIdAndNameAsync(
        string userId,
        string name,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(m => m.UserId == userId && m.Name == name, cancellationToken);
    }

    public async Task<IReadOnlyList<MetricDefinition>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await DbSet
            .Where(m => idList.Contains(m.Id))
            .ToListAsync(cancellationToken);
    }
}

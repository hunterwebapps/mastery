using Mastery.Domain.Entities.Experiment;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;
using Mastery.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mastery.Infrastructure.Repositories;

public class ExperimentRepository : BaseRepository<Experiment>, IExperimentRepository
{
    public ExperimentRepository(MasteryDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Experiment>> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(e => e.Notes)
            .Include(e => e.Result)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Experiment>> GetByUserIdAndStatusAsync(
        string userId,
        ExperimentStatus status,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(e => e.Notes)
            .Include(e => e.Result)
            .Where(e => e.UserId == userId && e.Status == status)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Experiment?> GetActiveByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(e => e.Notes)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Status == ExperimentStatus.Active, cancellationToken);
    }

    public async Task<Experiment?> GetByIdWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(e => e.Notes)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<bool> HasActiveExperimentAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(e => e.UserId == userId && e.Status == ExperimentStatus.Active, cancellationToken);
    }
}

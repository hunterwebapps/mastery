using Mastery.Domain.Entities;
using Mastery.Domain.Interfaces;
using Mastery.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mastery.Infrastructure.Repositories;

public class SeasonRepository : BaseRepository<Season>, ISeasonRepository
{
    public SeasonRepository(MasteryDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Season>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Season?> GetActiveSeasonForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.UserId == userId && s.ActualEndDate == null)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Season>> GetByUserIdAndDateRangeAsync(
        string userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.UserId == userId &&
                        s.StartDate >= startDate &&
                        s.StartDate <= endDate)
            .OrderByDescending(s => s.StartDate)
            .ToListAsync(cancellationToken);
    }
}

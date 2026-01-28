using Mastery.Domain.Entities.CheckIn;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;
using Mastery.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mastery.Infrastructure.Repositories;

public class CheckInRepository : BaseRepository<CheckIn>, ICheckInRepository
{
    public CheckInRepository(MasteryDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<CheckIn>> GetByUserIdAndDateAsync(
        string userId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.UserId == userId && c.CheckInDate == date)
            .OrderBy(c => c.Type)
            .ToListAsync(cancellationToken);
    }

    public async Task<CheckIn?> GetByUserIdAndDateAndTypeAsync(
        string userId,
        DateOnly date,
        CheckInType type,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(
                c => c.UserId == userId && c.CheckInDate == date && c.Type == type,
                cancellationToken);
    }

    public async Task<IReadOnlyList<CheckIn>> GetByUserIdAndDateRangeAsync(
        string userId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.UserId == userId
                && c.CheckInDate >= fromDate
                && c.CheckInDate <= toDate)
            .OrderByDescending(c => c.CheckInDate)
            .ThenBy(c => c.Type)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CheckIn>> GetTodayStateAsync(
        string userId,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.UserId == userId && c.CheckInDate == today)
            .OrderBy(c => c.Type)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByUserIdAndDateAndTypeAsync(
        string userId,
        DateOnly date,
        CheckInType type,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(
                c => c.UserId == userId && c.CheckInDate == date && c.Type == type,
                cancellationToken);
    }

    public async Task<int> CalculateStreakAsync(
        string userId,
        DateOnly upToDate,
        CancellationToken cancellationToken = default)
    {
        // Get completed check-ins ordered by date descending
        var completedDates = await DbSet
            .Where(c => c.UserId == userId
                && c.Status == CheckInStatus.Completed
                && c.CheckInDate <= upToDate)
            .Select(c => c.CheckInDate)
            .Distinct()
            .OrderByDescending(d => d)
            .Take(365)
            .ToListAsync(cancellationToken);

        if (completedDates.Count == 0)
            return 0;

        var streak = 0;
        var expectedDate = upToDate;

        foreach (var date in completedDates)
        {
            if (date == expectedDate)
            {
                streak++;
                expectedDate = expectedDate.AddDays(-1);
            }
            else if (date < expectedDate)
            {
                // Gap found - if today hasn't been completed yet, start from yesterday
                if (streak == 0 && expectedDate == upToDate)
                {
                    expectedDate = upToDate.AddDays(-1);
                    if (date == expectedDate)
                    {
                        streak++;
                        expectedDate = expectedDate.AddDays(-1);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        return streak;
    }
}

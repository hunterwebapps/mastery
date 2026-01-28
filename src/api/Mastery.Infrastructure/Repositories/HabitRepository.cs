using Mastery.Domain.Entities.Habit;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;
using Mastery.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mastery.Infrastructure.Repositories;

public class HabitRepository : BaseRepository<Habit>, IHabitRepository
{
    public HabitRepository(MasteryDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Habit>> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(h => h.MetricBindings)
            .Include(h => h.Variants)
            .Where(h => h.UserId == userId)
            .OrderBy(h => h.DisplayOrder)
            .ThenByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Habit>> GetActiveByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(h => h.MetricBindings)
            .Include(h => h.Variants)
            .Where(h => h.UserId == userId && h.Status == HabitStatus.Active)
            .OrderBy(h => h.DisplayOrder)
            .ThenByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Habit>> GetByStatusAsync(
        string userId,
        HabitStatus status,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(h => h.MetricBindings)
            .Include(h => h.Variants)
            .Where(h => h.UserId == userId && h.Status == status)
            .OrderBy(h => h.DisplayOrder)
            .ThenByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Habit?> GetByIdWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(h => h.MetricBindings)
            .Include(h => h.Variants)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
    }

    public async Task<Habit?> GetByIdWithOccurrencesAsync(
        Guid id,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(h => h.MetricBindings)
            .Include(h => h.Variants)
            .Include(h => h.Occurrences.Where(o => o.ScheduledOn >= fromDate && o.ScheduledOn <= toDate))
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Habit>> GetDueOnDateAsync(
        string userId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        // Get all active habits and filter by schedule in memory
        // This is necessary because the schedule logic is complex
        var activeHabits = await DbSet
            .Include(h => h.MetricBindings)
            .Include(h => h.Variants)
            .Include(h => h.Occurrences.Where(o => o.ScheduledOn == date))
            .Where(h => h.UserId == userId && h.Status == HabitStatus.Active)
            .OrderBy(h => h.DisplayOrder)
            .ToListAsync(cancellationToken);

        return activeHabits
            .Where(h => h.IsDueOn(date))
            .ToList();
    }

    public async Task<bool> ExistsByIdAndUserIdAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(h => h.Id == id && h.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Habit>> GetTodayHabitsAsync(
        string userId,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        // Optimized query for Today view
        var habits = await DbSet
            .Include(h => h.MetricBindings)
            .Include(h => h.Variants)
            .Include(h => h.Occurrences.Where(o => o.ScheduledOn == today))
            .Where(h => h.UserId == userId && h.Status == HabitStatus.Active)
            .OrderBy(h => h.DisplayOrder)
            .ToListAsync(cancellationToken);

        // Filter by schedule and include habits that are due today
        // or already have an occurrence for today (even if not strictly due)
        return habits
            .Where(h => h.IsDueOn(today) || h.Occurrences.Any(o => o.ScheduledOn == today))
            .ToList();
    }

    public async Task<Dictionary<Guid, int>> GetStreaksAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var habits = await DbSet
            .Where(h => h.UserId == userId && h.Status == HabitStatus.Active)
            .Select(h => new { h.Id, h.CurrentStreak })
            .ToListAsync(cancellationToken);

        return habits.ToDictionary(h => h.Id, h => h.CurrentStreak);
    }

    public async Task<Dictionary<Guid, decimal>> GetAdherenceRatesAsync(
        string userId,
        int days,
        CancellationToken cancellationToken = default)
    {
        var habits = await DbSet
            .Where(h => h.UserId == userId && h.Status == HabitStatus.Active)
            .Select(h => new { h.Id, h.AdherenceRate7Day })
            .ToListAsync(cancellationToken);

        return habits.ToDictionary(h => h.Id, h => h.AdherenceRate7Day);
    }

    public async Task<int> GetMaxDisplayOrderAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var maxOrder = await DbSet
            .Where(h => h.UserId == userId)
            .MaxAsync(h => (int?)h.DisplayOrder, cancellationToken);

        return maxOrder ?? -1;
    }

    public async Task<int> CalculateStreakAsync(
        Guid habitId,
        CancellationToken cancellationToken = default)
    {
        var habit = await DbSet
            .Include(h => h.Occurrences)
            .FirstOrDefaultAsync(h => h.Id == habitId, cancellationToken);

        if (habit == null)
            return 0;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var streak = 0;
        var currentDate = today;

        // Work backwards from today counting consecutive completions
        // For daily habits: check each day
        // For other schedules: check only scheduled days
        while (true)
        {
            if (!habit.Schedule.IsDueOn(currentDate))
            {
                // Not a scheduled day, skip
                currentDate = currentDate.AddDays(-1);
                continue;
            }

            var occurrence = habit.Occurrences
                .FirstOrDefault(o => o.ScheduledOn == currentDate);

            if (occurrence == null || occurrence.Status != HabitOccurrenceStatus.Completed)
            {
                // If it's today and not completed yet, don't break the streak
                if (currentDate == today)
                {
                    currentDate = currentDate.AddDays(-1);
                    continue;
                }
                break;
            }

            streak++;
            currentDate = currentDate.AddDays(-1);

            // Safety limit to prevent infinite loops
            if (currentDate < today.AddDays(-365))
                break;
        }

        return streak;
    }

    public Task AddOccurrenceAsync(
        HabitOccurrence occurrence,
        CancellationToken cancellationToken = default)
    {
        // Explicitly add to context to ensure proper change tracking
        // when parent entity was loaded with filtered includes
        Context.Set<HabitOccurrence>().Add(occurrence);
        return Task.CompletedTask;
    }
}

using Mastery.Domain.Entities.Project;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;
using Mastery.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using TaskStatus = Mastery.Domain.Enums.TaskStatus;

namespace Mastery.Infrastructure.Repositories;

public class ProjectRepository : BaseRepository<Project>, IProjectRepository
{
    public ProjectRepository(MasteryDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Project>> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Milestones)
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.Priority)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetByStatusAsync(
        string userId,
        ProjectStatus status,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Milestones)
            .Where(p => p.UserId == userId && p.Status == status)
            .OrderBy(p => p.Priority)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetByGoalIdAsync(
        Guid goalId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Milestones)
            .Where(p => p.GoalId == goalId)
            .OrderBy(p => p.Priority)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetActiveWithoutNextActionAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Milestones)
            .Where(p => p.UserId == userId)
            .Where(p => p.Status == ProjectStatus.Active)
            .Where(p => p.NextTaskId == null)
            .OrderBy(p => p.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<Project?> GetByIdWithMilestonesAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Milestones.OrderBy(m => m.DisplayOrder))
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Project?> GetByIdWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Milestones.OrderBy(m => m.DisplayOrder))
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByIdAndUserIdAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(p => p.Id == id && p.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetActiveWithSummaryAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Milestones)
            .Where(p => p.UserId == userId)
            .Where(p => p.Status == ProjectStatus.Active || p.Status == ProjectStatus.Draft)
            .OrderBy(p => p.Priority)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<TaskStatus, int>> GetTaskCountsByStatusAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        // Query tasks table to get counts by status for this project
        var counts = await Context.Set<Domain.Entities.Task.Task>()
            .Where(t => t.ProjectId == projectId)
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(c => c.Status, c => c.Count);
    }

    public async Task<IReadOnlyList<Project>> GetNearingDeadlineAsync(
        string userId,
        DateOnly referenceDate,
        int daysAhead,
        CancellationToken cancellationToken = default)
    {
        var deadlineThreshold = referenceDate.AddDays(daysAhead);

        return await DbSet
            .Include(p => p.Milestones)
            .Where(p => p.UserId == userId)
            .Where(p => p.Status == ProjectStatus.Active)
            .Where(p => p.TargetEndDate != null && p.TargetEndDate <= deadlineThreshold)
            .OrderBy(p => p.TargetEndDate)
            .ToListAsync(cancellationToken);
    }
}

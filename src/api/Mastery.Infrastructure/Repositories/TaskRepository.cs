using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;
using Mastery.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Task = Mastery.Domain.Entities.Task.Task;
using TaskStatus = Mastery.Domain.Enums.TaskStatus;

namespace Mastery.Infrastructure.Repositories;

public class TaskRepository : BaseRepository<Task>, ITaskRepository
{
    public TaskRepository(MasteryDbContext context) : base(context)
    {
    }

    public async System.Threading.Tasks.Task<IReadOnlyList<Task>> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(t => t.MetricBindings)
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.DisplayOrder)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task<IReadOnlyList<Task>> GetByStatusAsync(
        string userId,
        TaskStatus status,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(t => t.MetricBindings)
            .Where(t => t.UserId == userId && t.Status == status)
            .OrderBy(t => t.DisplayOrder)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task<IReadOnlyList<Task>> GetByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(t => t.MetricBindings)
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.DisplayOrder)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task<IReadOnlyList<Task>> GetByGoalIdAsync(
        Guid goalId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(t => t.MetricBindings)
            .Where(t => t.GoalId == goalId)
            .OrderBy(t => t.DisplayOrder)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task<IReadOnlyList<Task>> GetTodayTasksAsync(
        string userId,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        // Load all relevant tasks and filter in memory due to JSON column limitations
        var tasks = await DbSet
            .Include(t => t.MetricBindings)
            .Where(t => t.UserId == userId)
            .Where(t =>
                t.Status == TaskStatus.Scheduled ||
                t.Status == TaskStatus.Ready ||
                t.Status == TaskStatus.InProgress)
            .ToListAsync(cancellationToken);

        // Filter for today's tasks:
        // 1. Scheduled for today
        // 2. Due today (soft or hard)
        // 3. Overdue with hard due dates
        // 4. All Ready and InProgress tasks
        return tasks
            .Where(t =>
                (t.Status == TaskStatus.Scheduled && t.Scheduling?.IsScheduledFor(today) == true) ||
                t.Due?.IsDueOn(today) == true ||
                t.Due?.IsOverdue(today) == true ||
                t.Status == TaskStatus.Ready ||
                t.Status == TaskStatus.InProgress)
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.DisplayOrder)
            .ToList();
    }

    public async System.Threading.Tasks.Task<IReadOnlyList<Task>> GetInboxTasksAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(t => t.MetricBindings)
            .Where(t => t.UserId == userId && t.Status == TaskStatus.Inbox)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task<IReadOnlyList<Task>> GetOverdueTasksAsync(
        string userId,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        // Load all active tasks with due dates and filter in memory
        // due to JSON column limitations
        var tasks = await DbSet
            .Include(t => t.MetricBindings)
            .Where(t => t.UserId == userId)
            .Where(t => t.Status == TaskStatus.Ready || t.Status == TaskStatus.Scheduled)
            .ToListAsync(cancellationToken);

        return tasks
            .Where(t => t.Due?.IsOverdue(today) == true)
            .OrderBy(t => t.Priority)
            .ToList();
    }

    public async System.Threading.Tasks.Task<IReadOnlyList<Task>> GetReadyTasksAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(t => t.MetricBindings)
            .Where(t => t.UserId == userId && t.Status == TaskStatus.Ready)
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async System.Threading.Tasks.Task<Task?> GetByIdWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(t => t.MetricBindings)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async System.Threading.Tasks.Task<IReadOnlyList<Guid>> GetUnresolvedDependenciesAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var task = await DbSet
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task == null || !task.HasDependencies)
            return Array.Empty<Guid>();

        // Get dependency tasks that are not completed or cancelled
        var unresolvedDeps = await DbSet
            .Where(t => task.DependencyTaskIds.Contains(t.Id))
            .Where(t => t.Status != TaskStatus.Completed && t.Status != TaskStatus.Cancelled)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        return unresolvedDeps;
    }

    public async System.Threading.Tasks.Task<bool> IsBlockedAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var unresolvedDeps = await GetUnresolvedDependenciesAsync(taskId, cancellationToken);
        return unresolvedDeps.Count > 0;
    }

    public async System.Threading.Tasks.Task<bool> ExistsByIdAndUserIdAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(t => t.Id == id && t.UserId == userId, cancellationToken);
    }

    public async System.Threading.Tasks.Task<int> GetMaxDisplayOrderAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var maxOrder = await DbSet
            .Where(t => t.UserId == userId)
            .MaxAsync(t => (int?)t.DisplayOrder, cancellationToken);

        return maxOrder ?? -1;
    }

    public async System.Threading.Tasks.Task<IReadOnlyList<Task>> GetNBAEligibleTasksAsync(
        string userId,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        // Get Ready and Scheduled tasks
        var tasks = await DbSet
            .Include(t => t.MetricBindings)
            .Where(t => t.UserId == userId)
            .Where(t => t.Status == TaskStatus.Ready || t.Status == TaskStatus.Scheduled)
            .ToListAsync(cancellationToken);

        // Filter out blocked tasks
        var eligibleTasks = new List<Task>();
        foreach (var task in tasks)
        {
            var isBlocked = await IsBlockedAsync(task.Id, cancellationToken);
            if (!isBlocked)
            {
                eligibleTasks.Add(task);
            }
        }

        return eligibleTasks
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.EnergyCost)
            .ThenBy(t => t.DisplayOrder)
            .ToList();
    }

    public async System.Threading.Tasks.Task<IReadOnlyList<Task>> GetByContextTagAsync(
        string userId,
        ContextTag contextTag,
        CancellationToken cancellationToken = default)
    {
        // Load tasks and filter by context tag in memory
        var tasks = await DbSet
            .Include(t => t.MetricBindings)
            .Where(t => t.UserId == userId)
            .Where(t => t.Status == TaskStatus.Ready || t.Status == TaskStatus.Scheduled)
            .ToListAsync(cancellationToken);

        return tasks
            .Where(t => t.ContextTags.Contains(contextTag))
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.DisplayOrder)
            .ToList();
    }

    public async System.Threading.Tasks.Task<IReadOnlyList<Task>> GetCompletedInRangeAsync(
        string userId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        // Load completed tasks and filter by completion date in memory
        var tasks = await DbSet
            .Include(t => t.MetricBindings)
            .Where(t => t.UserId == userId && t.Status == TaskStatus.Completed)
            .ToListAsync(cancellationToken);

        return tasks
            .Where(t => t.Completion != null &&
                        t.Completion.CompletedOn >= fromDate &&
                        t.Completion.CompletedOn <= toDate)
            .OrderByDescending(t => t.Completion!.CompletedOn)
            .ToList();
    }
}

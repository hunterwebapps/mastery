using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Tasks.Models;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Tasks.Queries.GetTodayTasks;

public sealed class GetTodayTasksQueryHandler : IQueryHandler<GetTodayTasksQuery, List<TodayTaskDto>>
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IGoalRepository _goalRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetTodayTasksQueryHandler(
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        IGoalRepository goalRepository,
        ICurrentUserService currentUserService)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _goalRepository = goalRepository;
        _currentUserService = currentUserService;
    }

    public async Task<List<TodayTaskDto>> Handle(GetTodayTasksQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Get today's tasks - scheduled for today + due today + overdue + ready + in progress
        var tasks = await _taskRepository.GetTodayTasksAsync(userId, today, cancellationToken);

        // Get project and goal names for context
        var projectIds = tasks.Where(t => t.ProjectId.HasValue).Select(t => t.ProjectId!.Value).Distinct().ToList();
        var goalIds = tasks.Where(t => t.GoalId.HasValue).Select(t => t.GoalId!.Value).Distinct().ToList();

        var projects = projectIds.Count > 0
            ? await _projectRepository.GetByUserIdAsync(userId, cancellationToken)
            : [];
        var projectNameMap = projects.ToDictionary(p => p.Id, p => p.Title);

        var goals = goalIds.Count > 0
            ? await _goalRepository.GetActiveGoalsByUserIdAsync(userId, cancellationToken)
            : [];
        var goalNameMap = goals.ToDictionary(g => g.Id, g => g.Title);

        // Check blocked status for each task
        var result = new List<TodayTaskDto>();
        foreach (var task in tasks)
        {
            var isBlocked = await _taskRepository.IsBlockedAsync(task.Id, cancellationToken);
            result.Add(MapToTodayDto(task, projectNameMap, goalNameMap, isBlocked, today));
        }

        // Order: Overdue first, then by priority, then by scheduled time
        return result
            .OrderByDescending(t => t.IsOverdue)
            .ThenBy(t => t.Priority)
            .ThenBy(t => t.DisplayOrder)
            .ToList();
    }

    private static TodayTaskDto MapToTodayDto(
        Domain.Entities.Task.Task task,
        Dictionary<Guid, string> projectNameMap,
        Dictionary<Guid, string> goalNameMap,
        bool isBlocked,
        DateOnly today)
    {
        return new TodayTaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status.ToString(),
            Priority = task.Priority,
            EstimatedMinutes = task.EstimatedMinutes,
            EnergyCost = task.EnergyCost,
            DisplayOrder = task.DisplayOrder,
            DueOn = task.Due?.DueOn?.ToString("yyyy-MM-dd"),
            DueType = task.Due?.DueType.ToString(),
            ScheduledOn = task.Scheduling?.ScheduledOn.ToString("yyyy-MM-dd"),
            ContextTags = task.ContextTags.Select(t => t.ToString()).ToList(),
            IsOverdue = task.Due?.IsOverdue(today) ?? false,
            IsBlocked = isBlocked,
            RequiresValueEntry = task.RequiresValueEntry,
            RescheduleCount = task.RescheduleCount,
            ProjectId = task.ProjectId,
            ProjectTitle = task.ProjectId.HasValue && projectNameMap.TryGetValue(task.ProjectId.Value, out var pn) ? pn : null,
            GoalId = task.GoalId,
            GoalTitle = task.GoalId.HasValue && goalNameMap.TryGetValue(task.GoalId.Value, out var gn) ? gn : null,
            DependencyTaskIds = task.DependencyTaskIds.ToList()
        };
    }
}

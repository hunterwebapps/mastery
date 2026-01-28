using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Tasks.Models;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;
using TaskStatus = Mastery.Domain.Enums.TaskStatus;

namespace Mastery.Application.Features.Tasks.Queries.GetTasks;

public sealed class GetTasksQueryHandler : IQueryHandler<GetTasksQuery, List<TaskSummaryDto>>
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IGoalRepository _goalRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetTasksQueryHandler(
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

    public async Task<List<TaskSummaryDto>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        IReadOnlyList<Domain.Entities.Task.Task> tasks;

        // Filter by status if provided
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<TaskStatus>(request.Status, out var status))
        {
            tasks = await _taskRepository.GetByStatusAsync(userId, status, cancellationToken);
        }
        else if (request.ProjectId.HasValue)
        {
            tasks = await _taskRepository.GetByProjectIdAsync(request.ProjectId.Value, cancellationToken);
            tasks = tasks.Where(t => t.UserId == userId).ToList();
        }
        else if (request.GoalId.HasValue)
        {
            tasks = await _taskRepository.GetByGoalIdAsync(request.GoalId.Value, cancellationToken);
            tasks = tasks.Where(t => t.UserId == userId).ToList();
        }
        else if (!string.IsNullOrEmpty(request.ContextTag) && Enum.TryParse<ContextTag>(request.ContextTag, out var contextTag))
        {
            tasks = await _taskRepository.GetByContextTagAsync(userId, contextTag, cancellationToken);
        }
        else
        {
            tasks = await _taskRepository.GetByUserIdAsync(userId, cancellationToken);
        }

        // Additional overdue filter if requested
        if (request.IsOverdue == true)
        {
            tasks = tasks.Where(t => t.Due?.IsOverdue(today) == true).ToList();
        }

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

        return tasks.Select(task => MapToSummaryDto(task, projectNameMap, goalNameMap, today)).ToList();
    }

    private static TaskSummaryDto MapToSummaryDto(
        Domain.Entities.Task.Task task,
        Dictionary<Guid, string> projectNameMap,
        Dictionary<Guid, string> goalNameMap,
        DateOnly today)
    {
        return new TaskSummaryDto
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
            IsBlocked = task.HasDependencies,
            HasDependencies = task.HasDependencies,
            RescheduleCount = task.RescheduleCount,
            ProjectId = task.ProjectId,
            ProjectTitle = task.ProjectId.HasValue && projectNameMap.TryGetValue(task.ProjectId.Value, out var pn) ? pn : null,
            GoalId = task.GoalId,
            GoalTitle = task.GoalId.HasValue && goalNameMap.TryGetValue(task.GoalId.Value, out var gn) ? gn : null,
            CreatedAt = task.CreatedAt
        };
    }
}

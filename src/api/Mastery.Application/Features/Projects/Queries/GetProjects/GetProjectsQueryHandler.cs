using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Projects.Models;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Projects.Queries.GetProjects;

public sealed class GetProjectsQueryHandler : IQueryHandler<GetProjectsQuery, List<ProjectSummaryDto>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IGoalRepository _goalRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetProjectsQueryHandler(
        IProjectRepository projectRepository,
        ITaskRepository taskRepository,
        IGoalRepository goalRepository,
        ICurrentUserService currentUserService)
    {
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _goalRepository = goalRepository;
        _currentUserService = currentUserService;
    }

    public async Task<List<ProjectSummaryDto>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        IReadOnlyList<Domain.Entities.Project.Project> projects;

        // Filter by status if provided
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<ProjectStatus>(request.Status, out var status))
        {
            projects = await _projectRepository.GetByStatusAsync(userId, status, cancellationToken);
        }
        else if (request.GoalId.HasValue)
        {
            projects = await _projectRepository.GetByGoalIdAsync(request.GoalId.Value, cancellationToken);
            projects = projects.Where(p => p.UserId == userId).ToList();
        }
        else
        {
            projects = await _projectRepository.GetByUserIdAsync(userId, cancellationToken);
        }

        // Get goal names for context
        var goalIds = projects.Where(p => p.GoalId.HasValue).Select(p => p.GoalId!.Value).Distinct().ToList();
        var goals = goalIds.Count > 0
            ? await _goalRepository.GetActiveGoalsByUserIdAsync(userId, cancellationToken)
            : [];
        var goalNameMap = goals.ToDictionary(g => g.Id, g => g.Title);

        // Get task counts and next task titles
        var result = new List<ProjectSummaryDto>();
        foreach (var project in projects)
        {
            var taskCounts = await _projectRepository.GetTaskCountsByStatusAsync(project.Id, cancellationToken);
            var totalTasks = taskCounts.Values.Sum();
            var completedTasks = taskCounts.GetValueOrDefault(Domain.Enums.TaskStatus.Completed, 0);
            var inProgressTasks = taskCounts.GetValueOrDefault(Domain.Enums.TaskStatus.InProgress, 0);
            var readyTasks = taskCounts.GetValueOrDefault(Domain.Enums.TaskStatus.Ready, 0);
            var scheduledTasks = taskCounts.GetValueOrDefault(Domain.Enums.TaskStatus.Scheduled, 0);
            var cancelledTasks = taskCounts.GetValueOrDefault(Domain.Enums.TaskStatus.Cancelled, 0);
            var archivedTasks = taskCounts.GetValueOrDefault(Domain.Enums.TaskStatus.Archived, 0);

            // A project is stuck if it's active and has no actionable tasks,
            // UNLESS all tasks AND all milestones are completed (ready to complete the project)
            var incompleteTasks = totalTasks - completedTasks - cancelledTasks - archivedTasks;
            var actionableTasks = readyTasks + inProgressTasks + scheduledTasks;
            var allTasksCompleted = totalTasks > 0 && incompleteTasks == 0;
            var allMilestonesCompleted = project.Milestones.Count == 0 || project.CompletedMilestonesCount == project.Milestones.Count;
            var isReadyToComplete = allTasksCompleted && allMilestonesCompleted;
            var isStuck = project.Status == ProjectStatus.Active
                && actionableTasks == 0
                && !isReadyToComplete;

            string? nextTaskTitle = null;
            if (project.NextTaskId.HasValue)
            {
                var nextTask = await _taskRepository.GetByIdAsync(project.NextTaskId.Value, cancellationToken);
                nextTaskTitle = nextTask?.Title;
            }

            result.Add(MapToSummaryDto(project, goalNameMap, nextTaskTitle, totalTasks, completedTasks, isStuck, today));
        }

        return result;
    }

    private static ProjectSummaryDto MapToSummaryDto(
        Domain.Entities.Project.Project project,
        Dictionary<Guid, string> goalNameMap,
        string? nextTaskTitle,
        int totalTasks,
        int completedTasks,
        bool isStuck,
        DateOnly today)
    {
        var daysUntilDeadline = project.TargetEndDate.HasValue
            ? project.TargetEndDate.Value.DayNumber - today.DayNumber
            : (int?)null;

        return new ProjectSummaryDto
        {
            Id = project.Id,
            Title = project.Title,
            Description = project.Description,
            Status = project.Status.ToString(),
            Priority = project.Priority,
            GoalId = project.GoalId,
            GoalTitle = project.GoalId.HasValue && goalNameMap.TryGetValue(project.GoalId.Value, out var gn) ? gn : null,
            TargetEndDate = project.TargetEndDate?.ToString("yyyy-MM-dd"),
            NextTaskId = project.NextTaskId,
            NextTaskTitle = nextTaskTitle,
            TotalTasks = totalTasks,
            CompletedTasks = completedTasks,
            MilestoneCount = project.Milestones.Count,
            CompletedMilestones = project.CompletedMilestonesCount,
            IsStuck = isStuck,
            IsNearingDeadline = daysUntilDeadline.HasValue && daysUntilDeadline <= 7 && daysUntilDeadline > 0,
            CreatedAt = project.CreatedAt
        };
    }
}

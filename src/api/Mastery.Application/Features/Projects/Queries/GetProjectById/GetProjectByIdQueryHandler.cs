using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Projects.Models;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Projects.Queries.GetProjectById;

public sealed class GetProjectByIdQueryHandler : IQueryHandler<GetProjectByIdQuery, ProjectDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IGoalRepository _goalRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetProjectByIdQueryHandler(
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

    public async Task<ProjectDto> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var project = await _projectRepository.GetByIdWithMilestonesAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Project.Project), request.Id);

        if (project.UserId != userId)
            throw new NotFoundException(nameof(Domain.Entities.Project.Project), request.Id);

        // Get task counts
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
        var allMilestonesCompleted = project.Milestones.Count == 0 || project.Milestones.All(m => m.Status == Domain.Enums.MilestoneStatus.Completed);
        var isReadyToComplete = allTasksCompleted && allMilestonesCompleted;
        var isStuck = project.Status == Domain.Enums.ProjectStatus.Active
            && actionableTasks == 0
            && !isReadyToComplete;

        // Get goal name if linked
        string? goalTitle = null;
        if (project.GoalId.HasValue)
        {
            var goal = await _goalRepository.GetByIdAsync(project.GoalId.Value, cancellationToken);
            goalTitle = goal?.Title;
        }

        // Get next task title if set
        string? nextTaskTitle = null;
        if (project.NextTaskId.HasValue)
        {
            var nextTask = await _taskRepository.GetByIdAsync(project.NextTaskId.Value, cancellationToken);
            nextTaskTitle = nextTask?.Title;
        }

        return MapToDto(project, goalTitle, nextTaskTitle, totalTasks, completedTasks, inProgressTasks, isStuck);
    }

    private static ProjectDto MapToDto(
        Domain.Entities.Project.Project project,
        string? goalTitle,
        string? nextTaskTitle,
        int totalTasks,
        int completedTasks,
        int inProgressTasks,
        bool isStuck)
    {
        return new ProjectDto
        {
            Id = project.Id,
            UserId = project.UserId,
            Title = project.Title,
            Description = project.Description,
            Status = project.Status.ToString(),
            Priority = project.Priority,
            GoalId = project.GoalId,
            GoalTitle = goalTitle,
            SeasonId = project.SeasonId,
            TargetEndDate = project.TargetEndDate?.ToString("yyyy-MM-dd"),
            NextTaskId = project.NextTaskId,
            NextTaskTitle = nextTaskTitle,
            RoleIds = project.RoleIds.ToList(),
            ValueIds = project.ValueIds.ToList(),
            Milestones = project.Milestones.Select(m => new MilestoneDto
            {
                Id = m.Id,
                ProjectId = m.ProjectId,
                Title = m.Title,
                TargetDate = m.TargetDate?.ToString("yyyy-MM-dd"),
                Status = m.Status.ToString(),
                Notes = m.Notes,
                DisplayOrder = m.DisplayOrder,
                CompletedAtUtc = m.CompletedAtUtc,
                CreatedAt = m.CreatedAt
            }).ToList(),
            OutcomeNotes = project.OutcomeNotes,
            CompletedAtUtc = project.CompletedAtUtc,
            TotalTasks = totalTasks,
            CompletedTasks = completedTasks,
            InProgressTasks = inProgressTasks,
            IsStuck = isStuck,
            CreatedAt = project.CreatedAt,
            ModifiedAt = project.ModifiedAt
        };
    }
}

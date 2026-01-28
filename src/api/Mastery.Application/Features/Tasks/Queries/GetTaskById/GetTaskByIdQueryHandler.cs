using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Tasks.Models;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Tasks.Queries.GetTaskById;

public sealed class GetTaskByIdQueryHandler : IQueryHandler<GetTaskByIdQuery, TaskDto>
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IGoalRepository _goalRepository;
    private readonly IMetricDefinitionRepository _metricDefinitionRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetTaskByIdQueryHandler(
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        IGoalRepository goalRepository,
        IMetricDefinitionRepository metricDefinitionRepository,
        ICurrentUserService currentUserService)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _goalRepository = goalRepository;
        _metricDefinitionRepository = metricDefinitionRepository;
        _currentUserService = currentUserService;
    }

    public async Task<TaskDto> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var task = await _taskRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Task.Task), request.Id);

        if (task.UserId != userId)
            throw new NotFoundException(nameof(Domain.Entities.Task.Task), request.Id);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var isBlocked = await _taskRepository.IsBlockedAsync(task.Id, cancellationToken);

        // Get project and goal names
        string? projectTitle = null;
        string? goalTitle = null;

        if (task.ProjectId.HasValue)
        {
            var project = await _projectRepository.GetByIdAsync(task.ProjectId.Value, cancellationToken);
            projectTitle = project?.Title;
        }

        if (task.GoalId.HasValue)
        {
            var goal = await _goalRepository.GetByIdAsync(task.GoalId.Value, cancellationToken);
            goalTitle = goal?.Title;
        }

        // Get metric names if there are bindings
        Dictionary<Guid, string> metricNames = new();
        if (task.MetricBindings.Count > 0)
        {
            var metricIds = task.MetricBindings.Select(b => b.MetricDefinitionId).ToList();
            var metrics = await _metricDefinitionRepository.GetByIdsAsync(metricIds, cancellationToken);
            metricNames = metrics.ToDictionary(m => m.Id, m => m.Name);
        }

        return MapToDto(task, metricNames, isBlocked, today, projectTitle, goalTitle);
    }

    private static TaskDto MapToDto(
        Domain.Entities.Task.Task task,
        Dictionary<Guid, string> metricNames,
        bool isBlocked,
        DateOnly today,
        string? projectTitle,
        string? goalTitle)
    {
        return new TaskDto
        {
            Id = task.Id,
            UserId = task.UserId,
            ProjectId = task.ProjectId,
            ProjectTitle = projectTitle,
            GoalId = task.GoalId,
            GoalTitle = goalTitle,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status.ToString(),
            Priority = task.Priority,
            EstimatedMinutes = task.EstimatedMinutes,
            EnergyCost = task.EnergyCost,
            DisplayOrder = task.DisplayOrder,
            Due = task.Due != null ? new TaskDueDto
            {
                DueOn = task.Due.DueOn?.ToString("yyyy-MM-dd"),
                DueAt = task.Due.DueAt?.ToString("HH:mm"),
                DueType = task.Due.DueType.ToString()
            } : null,
            Scheduling = task.Scheduling != null ? new TaskSchedulingDto
            {
                ScheduledOn = task.Scheduling.ScheduledOn.ToString("yyyy-MM-dd"),
                PreferredTimeWindowStart = task.Scheduling.PreferredTimeWindow?.Start.ToString("HH:mm"),
                PreferredTimeWindowEnd = task.Scheduling.PreferredTimeWindow?.End.ToString("HH:mm")
            } : null,
            Completion = task.Completion != null ? new TaskCompletionDto
            {
                CompletedAtUtc = task.Completion.CompletedAtUtc,
                CompletedOn = task.Completion.CompletedOn.ToString("yyyy-MM-dd"),
                ActualMinutes = task.Completion.ActualMinutes,
                CompletionNote = task.Completion.CompletionNote,
                EnteredValue = task.Completion.EnteredValue
            } : null,
            ContextTags = task.ContextTags.Select(t => t.ToString()).ToList(),
            DependencyTaskIds = task.DependencyTaskIds.ToList(),
            RoleIds = task.RoleIds.ToList(),
            ValueIds = task.ValueIds.ToList(),
            MetricBindings = task.MetricBindings.Select(b => new TaskMetricBindingDto
            {
                Id = b.Id,
                MetricDefinitionId = b.MetricDefinitionId,
                MetricName = metricNames.TryGetValue(b.MetricDefinitionId, out var name) ? name : null,
                ContributionType = b.ContributionType.ToString(),
                FixedValue = b.FixedValue,
                Notes = b.Notes
            }).ToList(),
            LastRescheduleReason = task.LastRescheduleReason?.ToString(),
            RescheduleCount = task.RescheduleCount,
            IsOverdue = task.Due?.IsOverdue(today) ?? false,
            IsBlocked = isBlocked,
            IsEligibleForNBA = task.IsEligibleForNBA && !isBlocked,
            CreatedAt = task.CreatedAt,
            ModifiedAt = task.ModifiedAt
        };
    }
}

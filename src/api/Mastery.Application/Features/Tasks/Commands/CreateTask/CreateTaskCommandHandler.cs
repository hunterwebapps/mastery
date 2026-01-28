using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;
using Mastery.Domain.ValueObjects;
using Task = Mastery.Domain.Entities.Task.Task;

namespace Mastery.Application.Features.Tasks.Commands.CreateTask;

public sealed class CreateTaskCommandHandler : ICommandHandler<CreateTaskCommand, Guid>
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IMetricDefinitionRepository _metricDefinitionRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTaskCommandHandler(
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        IMetricDefinitionRepository metricDefinitionRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _metricDefinitionRepository = metricDefinitionRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async System.Threading.Tasks.Task<Guid> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        // Validate project ownership if provided
        if (request.ProjectId.HasValue)
        {
            var projectExists = await _projectRepository.ExistsByIdAndUserIdAsync(
                request.ProjectId.Value, userId, cancellationToken);
            if (!projectExists)
                throw new NotFoundException(nameof(Domain.Entities.Project.Project), request.ProjectId.Value);
        }

        // Parse due date if provided
        TaskDue? due = null;
        if (request.Due != null)
        {
            if (!DateOnly.TryParse(request.Due.DueOn, out var dueOn))
                throw new DomainException($"Invalid due date: {request.Due.DueOn}");

            TimeOnly? dueAt = null;
            if (!string.IsNullOrEmpty(request.Due.DueAt) && TimeOnly.TryParse(request.Due.DueAt, out var t))
                dueAt = t;

            if (!Enum.TryParse<DueType>(request.Due.DueType, out var dueType))
                throw new DomainException($"Invalid due type: {request.Due.DueType}");

            due = TaskDue.Create(dueOn, dueAt, dueType);
        }

        // Parse scheduling if provided
        TaskScheduling? scheduling = null;
        if (request.Scheduling != null)
        {
            if (!DateOnly.TryParse(request.Scheduling.ScheduledOn, out var scheduledOn))
                throw new DomainException($"Invalid scheduled date: {request.Scheduling.ScheduledOn}");

            TimeWindow? timeWindow = null;
            if (!string.IsNullOrEmpty(request.Scheduling.PreferredTimeWindowStart) &&
                !string.IsNullOrEmpty(request.Scheduling.PreferredTimeWindowEnd))
            {
                if (TimeOnly.TryParse(request.Scheduling.PreferredTimeWindowStart, out var start) &&
                    TimeOnly.TryParse(request.Scheduling.PreferredTimeWindowEnd, out var end))
                {
                    timeWindow = TimeWindow.Create(start, end);
                }
            }

            scheduling = TaskScheduling.Create(scheduledOn, timeWindow);
        }

        // Parse context tags
        List<ContextTag>? contextTags = null;
        if (request.ContextTags?.Count > 0)
        {
            contextTags = request.ContextTags
                .Select(t => Enum.TryParse<ContextTag>(t, out var tag) ? tag : (ContextTag?)null)
                .Where(t => t.HasValue)
                .Select(t => t!.Value)
                .ToList();
        }

        // Get next display order
        var maxOrder = await _taskRepository.GetMaxDisplayOrderAsync(userId, cancellationToken);

        // Create the task
        var task = Task.Create(
            userId: userId,
            title: request.Title,
            estimatedMinutes: request.EstimatedMinutes,
            energyCost: request.EnergyCost,
            description: request.Description,
            priority: request.Priority,
            projectId: request.ProjectId,
            goalId: request.GoalId,
            due: due,
            scheduling: scheduling,
            contextTags: contextTags,
            dependencyTaskIds: request.DependencyTaskIds,
            roleIds: request.RoleIds,
            valueIds: request.ValueIds,
            displayOrder: maxOrder + 1,
            startAsReady: request.StartAsReady);

        // Add metric bindings if provided
        if (request.MetricBindings?.Count > 0)
        {
            var metricDefIds = request.MetricBindings.Select(b => b.MetricDefinitionId).Distinct().ToList();
            var metricDefs = await _metricDefinitionRepository.GetByIdsAsync(metricDefIds, cancellationToken);

            foreach (var bindingInput in request.MetricBindings)
            {
                var metricDef = metricDefs.FirstOrDefault(m => m.Id == bindingInput.MetricDefinitionId)
                    ?? throw new NotFoundException(nameof(Domain.Entities.Metrics.MetricDefinition), bindingInput.MetricDefinitionId);

                if (metricDef.UserId != userId)
                    throw new DomainException($"Metric definition {bindingInput.MetricDefinitionId} does not belong to the current user.");

                if (!Enum.TryParse<TaskContributionType>(bindingInput.ContributionType, out var contributionType))
                    throw new DomainException($"Invalid contribution type: {bindingInput.ContributionType}");

                task.AddMetricBinding(
                    metricDefinitionId: bindingInput.MetricDefinitionId,
                    contributionType: contributionType,
                    fixedValue: bindingInput.FixedValue,
                    notes: bindingInput.Notes);
            }
        }

        await _taskRepository.AddAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return task.Id;
    }
}

using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;
using Mastery.Domain.ValueObjects;

namespace Mastery.Application.Features.Tasks.Commands.UpdateTask;

public sealed class UpdateTaskCommandHandler : ICommandHandler<UpdateTaskCommand>
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTaskCommandHandler(
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Task.Task), request.TaskId);

        if (task.UserId != userId)
            throw new DomainException("You don't have permission to modify this task.");

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

        // Parse context tags
        List<ContextTag>? contextTags = null;
        if (request.ContextTags != null)
        {
            contextTags = request.ContextTags
                .Select(t => Enum.TryParse<ContextTag>(t, out var tag) ? tag : (ContextTag?)null)
                .Where(t => t.HasValue)
                .Select(t => t!.Value)
                .ToList();
        }

        // Update the task
        task.Update(
            title: request.Title,
            description: request.Description,
            priority: request.Priority,
            estimatedMinutes: request.EstimatedMinutes,
            energyCost: request.EnergyCost,
            projectId: request.ProjectId,
            goalId: request.GoalId,
            due: due,
            contextTags: contextTags,
            roleIds: request.RoleIds,
            valueIds: request.ValueIds);

        // Handle clearing due date separately
        if (request.ClearDue && request.Due == null)
        {
            task.ClearDue();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

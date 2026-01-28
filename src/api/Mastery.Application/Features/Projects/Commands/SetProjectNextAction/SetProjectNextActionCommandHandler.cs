using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Projects.Commands.SetProjectNextAction;

public sealed class SetProjectNextActionCommandHandler : ICommandHandler<SetProjectNextActionCommand>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public SetProjectNextActionCommandHandler(
        IProjectRepository projectRepository,
        ITaskRepository taskRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SetProjectNextActionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Project.Project), request.ProjectId);

        if (project.UserId != userId)
            throw new NotFoundException(nameof(Domain.Entities.Project.Project), request.ProjectId);

        if (request.TaskId.HasValue)
        {
            // Verify the task exists, belongs to the user, and belongs to this project
            var task = await _taskRepository.GetByIdAsync(request.TaskId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(Domain.Entities.Task.Task), request.TaskId.Value);

            if (task.UserId != userId)
                throw new NotFoundException(nameof(Domain.Entities.Task.Task), request.TaskId.Value);

            if (task.ProjectId != project.Id)
                throw new DomainException("The task does not belong to this project.");

            project.SetNextTask(request.TaskId.Value);
        }
        else
        {
            project.ClearNextTask();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

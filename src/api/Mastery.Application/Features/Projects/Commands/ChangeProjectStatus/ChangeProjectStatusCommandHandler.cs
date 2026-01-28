using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Projects.Commands.ChangeProjectStatus;

public sealed class ChangeProjectStatusCommandHandler : ICommandHandler<ChangeProjectStatusCommand>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeProjectStatusCommandHandler(
        IProjectRepository projectRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ChangeProjectStatusCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Project.Project), request.ProjectId);

        if (project.UserId != userId)
            throw new NotFoundException(nameof(Domain.Entities.Project.Project), request.ProjectId);

        if (!Enum.TryParse<ProjectStatus>(request.NewStatus, out var newStatus))
            throw new DomainException($"Invalid project status: {request.NewStatus}");

        switch (newStatus)
        {
            case ProjectStatus.Active:
                if (project.Status == ProjectStatus.Paused)
                    project.Resume();
                else if (project.Status == ProjectStatus.Completed)
                    project.Reactivate();
                else
                    project.Activate();
                break;

            case ProjectStatus.Paused:
                project.Pause();
                break;

            case ProjectStatus.Archived:
                project.Archive();
                break;

            default:
                throw new DomainException($"Cannot transition to status {newStatus} via this command. Use specific commands for Complete.");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Projects.Commands.UpdateProject;

public sealed class UpdateProjectCommandHandler : ICommandHandler<UpdateProjectCommand>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProjectCommandHandler(
        IProjectRepository projectRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Project.Project), request.ProjectId);

        if (project.UserId != userId)
            throw new NotFoundException(nameof(Domain.Entities.Project.Project), request.ProjectId);

        DateOnly? targetEndDate = null;
        if (!string.IsNullOrEmpty(request.TargetEndDate) && DateOnly.TryParse(request.TargetEndDate, out var parsedDate))
        {
            targetEndDate = parsedDate;
        }

        project.Update(
            title: request.Title,
            description: request.Description,
            priority: request.Priority,
            goalId: request.GoalId,
            seasonId: request.SeasonId,
            targetEndDate: targetEndDate);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

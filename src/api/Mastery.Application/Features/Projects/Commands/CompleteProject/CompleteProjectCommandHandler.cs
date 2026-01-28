using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Projects.Commands.CompleteProject;

public sealed class CompleteProjectCommandHandler : ICommandHandler<CompleteProjectCommand>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteProjectCommandHandler(
        IProjectRepository projectRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(CompleteProjectCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Project.Project), request.ProjectId);

        if (project.UserId != userId)
            throw new NotFoundException(nameof(Domain.Entities.Project.Project), request.ProjectId);

        project.Complete(request.OutcomeNotes);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

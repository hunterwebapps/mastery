using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Projects.Commands.UpdateMilestone;

public sealed class UpdateMilestoneCommandHandler : ICommandHandler<UpdateMilestoneCommand>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMilestoneCommandHandler(
        IProjectRepository projectRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateMilestoneCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var project = await _projectRepository.GetByIdWithMilestonesAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        if (project.UserId != userId)
            throw new DomainException("You don't have permission to modify this project.");

        DateOnly? targetDate = null;
        if (!string.IsNullOrEmpty(request.TargetDate) && DateOnly.TryParse(request.TargetDate, out var td))
            targetDate = td;

        project.UpdateMilestone(
            request.MilestoneId,
            request.Title,
            targetDate,
            request.Notes,
            request.DisplayOrder);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

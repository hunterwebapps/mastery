using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Project;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Projects.Commands.CreateProject;

public sealed class CreateProjectCommandHandler : ICommandHandler<CreateProjectCommand, Guid>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProjectCommandHandler(
        IProjectRepository projectRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        DateOnly? targetEndDate = null;
        if (!string.IsNullOrEmpty(request.TargetEndDate) && DateOnly.TryParse(request.TargetEndDate, out var ted))
            targetEndDate = ted;

        var project = Project.Create(
            userId: userId,
            title: request.Title,
            description: request.Description,
            priority: request.Priority,
            goalId: request.GoalId,
            seasonId: request.SeasonId,
            targetEndDate: targetEndDate,
            roleIds: request.RoleIds,
            valueIds: request.ValueIds,
            saveAsDraft: request.SaveAsDraft);

        // Add milestones if provided
        if (request.Milestones?.Count > 0)
        {
            foreach (var milestoneInput in request.Milestones)
            {
                DateOnly? milestoneDate = null;
                if (!string.IsNullOrEmpty(milestoneInput.TargetDate) && DateOnly.TryParse(milestoneInput.TargetDate, out var md))
                    milestoneDate = md;

                project.AddMilestone(milestoneInput.Title, milestoneDate, milestoneInput.Notes);
            }
        }

        await _projectRepository.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return project.Id;
    }
}

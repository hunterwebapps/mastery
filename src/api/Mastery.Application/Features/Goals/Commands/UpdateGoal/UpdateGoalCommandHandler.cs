using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Goals.Commands.UpdateGoal;

public sealed class UpdateGoalCommandHandler : ICommandHandler<UpdateGoalCommand>
{
    private readonly IGoalRepository _goalRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateGoalCommandHandler(
        IGoalRepository goalRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _goalRepository = goalRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateGoalCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var goal = await _goalRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Goal.Goal), request.Id);

        if (goal.UserId != userId)
            throw new DomainException("Goal does not belong to the current user.");

        goal.Update(
            title: request.Title,
            description: request.Description,
            why: request.Why,
            priority: request.Priority,
            deadline: request.Deadline,
            seasonId: request.SeasonId,
            roleIds: request.RoleIds,
            valueIds: request.ValueIds,
            dependencyIds: request.DependencyIds);

        await _goalRepository.UpdateAsync(goal, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

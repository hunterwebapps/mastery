using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Goals.Commands.UpdateGoalStatus;

public sealed class UpdateGoalStatusCommandHandler : ICommandHandler<UpdateGoalStatusCommand>
{
    private readonly IGoalRepository _goalRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateGoalStatusCommandHandler(
        IGoalRepository goalRepository,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _goalRepository = goalRepository;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateGoalStatusCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var goal = await _goalRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Goal.Goal), request.Id);

        if (goal.UserId != userId)
            throw new DomainException("Goal does not belong to the current user.");

        if (!Enum.TryParse<GoalStatus>(request.NewStatus, out var newStatus))
            throw new DomainException($"Invalid goal status: {request.NewStatus}");

        switch (newStatus)
        {
            case GoalStatus.Active:
                goal.Activate();
                break;
            case GoalStatus.Paused:
                goal.Pause();
                break;
            case GoalStatus.Completed:
                goal.Complete(request.CompletionNotes);
                break;
            case GoalStatus.Archived:
                goal.Archive();
                break;
            default:
                throw new DomainException($"Cannot change to status: {newStatus}");
        }

        await _goalRepository.UpdateAsync(goal, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

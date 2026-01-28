using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Goals.Commands.DeleteGoal;

public sealed class DeleteGoalCommandHandler : ICommandHandler<DeleteGoalCommand>
{
    private readonly IGoalRepository _goalRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteGoalCommandHandler(
        IGoalRepository goalRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _goalRepository = goalRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteGoalCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var goal = await _goalRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Goal.Goal), request.Id);

        if (goal.UserId != userId)
            throw new DomainException("Goal does not belong to the current user.");

        // Soft delete by archiving
        goal.Archive();

        await _goalRepository.UpdateAsync(goal, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

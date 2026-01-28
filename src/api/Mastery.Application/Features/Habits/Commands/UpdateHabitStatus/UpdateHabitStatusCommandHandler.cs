using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Habits.Commands.UpdateHabitStatus;

public sealed class UpdateHabitStatusCommandHandler : ICommandHandler<UpdateHabitStatusCommand>
{
    private readonly IHabitRepository _habitRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateHabitStatusCommandHandler(
        IHabitRepository habitRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _habitRepository = habitRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateHabitStatusCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var habit = await _habitRepository.GetByIdAsync(request.HabitId, cancellationToken);

        if (habit == null)
            throw new NotFoundException("Habit", request.HabitId);

        if (habit.UserId != userId)
            throw new DomainException("Habit does not belong to the current user.");

        if (!Enum.TryParse<HabitStatus>(request.Status, out var newStatus))
            throw new DomainException($"Invalid status: {request.Status}");

        switch (newStatus)
        {
            case HabitStatus.Active:
                habit.Activate();
                break;
            case HabitStatus.Paused:
                habit.Pause();
                break;
            case HabitStatus.Archived:
                habit.Archive();
                break;
            default:
                throw new DomainException($"Unsupported status transition to: {newStatus}");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

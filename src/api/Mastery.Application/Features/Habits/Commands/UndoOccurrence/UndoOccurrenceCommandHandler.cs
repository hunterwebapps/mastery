using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Habits.Commands.UndoOccurrence;

public sealed class UndoOccurrenceCommandHandler : ICommandHandler<UndoOccurrenceCommand>
{
    private readonly IHabitRepository _habitRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UndoOccurrenceCommandHandler(
        IHabitRepository habitRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _habitRepository = habitRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UndoOccurrenceCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        if (!DateOnly.TryParse(request.Date, out var date))
            throw new DomainException($"Invalid date format: {request.Date}");

        var habit = await _habitRepository.GetByIdWithOccurrencesAsync(
            request.HabitId,
            date,
            date,
            cancellationToken);

        if (habit == null)
            throw new NotFoundException("Habit", request.HabitId);

        if (habit.UserId != userId)
            throw new DomainException("Habit does not belong to the current user.");

        habit.UndoOccurrence(date);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Habits.Commands.SkipOccurrence;

public sealed class SkipOccurrenceCommandHandler : ICommandHandler<SkipOccurrenceCommand>
{
    private readonly IHabitRepository _habitRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public SkipOccurrenceCommandHandler(
        IHabitRepository habitRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _habitRepository = habitRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SkipOccurrenceCommand request, CancellationToken cancellationToken)
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

        if (!habit.Policy.AllowSkip)
            throw new DomainException("Skipping is not allowed for this habit.");

        // Check if occurrence already exists for this date
        var existingOccurrence = habit.GetOccurrence(date);
        var isNewOccurrence = existingOccurrence == null;

        habit.SkipOccurrence(date, request.Reason);

        // If this is a new occurrence, explicitly add it to the repository
        if (isNewOccurrence)
        {
            var newOccurrence = habit.GetOccurrence(date);
            if (newOccurrence != null)
            {
                await _habitRepository.AddOccurrenceAsync(newOccurrence, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

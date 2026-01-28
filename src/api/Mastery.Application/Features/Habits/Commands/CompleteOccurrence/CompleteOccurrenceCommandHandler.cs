using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Habit;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Habits.Commands.CompleteOccurrence;

public sealed class CompleteOccurrenceCommandHandler : ICommandHandler<CompleteOccurrenceCommand>
{
    private readonly IHabitRepository _habitRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteOccurrenceCommandHandler(
        IHabitRepository habitRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _habitRepository = habitRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(CompleteOccurrenceCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        // Parse date
        if (!DateOnly.TryParse(request.Date, out var date))
            throw new DomainException($"Invalid date format: {request.Date}");

        // Get habit with today's occurrence if it exists
        var habit = await _habitRepository.GetByIdWithOccurrencesAsync(
            request.HabitId,
            date,
            date,
            cancellationToken);

        if (habit == null)
            throw new NotFoundException("Habit", request.HabitId);

        if (habit.UserId != userId)
            throw new DomainException("Habit does not belong to the current user.");

        if (habit.Status != HabitStatus.Active)
            throw new DomainException("Cannot complete an occurrence for an inactive habit.");

        // Parse mode if provided
        HabitMode? mode = null;
        if (!string.IsNullOrEmpty(request.Mode))
        {
            if (!Enum.TryParse<HabitMode>(request.Mode, out var parsedMode))
                throw new DomainException($"Invalid mode: {request.Mode}");
            mode = parsedMode;
        }

        // Validate value if required
        if (habit.RequiresValueEntry && !request.Value.HasValue)
            throw new DomainException("This habit requires a value to be entered.");

        // Check backfill policy
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (date < today)
        {
            var daysDiff = today.DayNumber - date.DayNumber;
            if (!habit.Policy.AllowBackfill)
                throw new DomainException("Backfill is not allowed for this habit.");
            if (daysDiff > habit.Policy.MaxBackfillDays)
                throw new DomainException($"Cannot backfill more than {habit.Policy.MaxBackfillDays} days.");
        }

        // Check if occurrence already exists for this date
        var existingOccurrence = habit.GetOccurrence(date);
        var isNewOccurrence = existingOccurrence == null;

        // Complete the occurrence (creates new if needed)
        habit.CompleteOccurrence(date, request.Value, mode, request.Note);

        // If this is a new occurrence, explicitly add it to the repository
        // to ensure proper change tracking with filtered includes
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

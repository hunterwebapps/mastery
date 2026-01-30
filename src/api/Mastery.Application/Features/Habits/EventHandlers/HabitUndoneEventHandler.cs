using MediatR;
using Mastery.Domain.Entities.Habit;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Habits.EventHandlers;

/// <summary>
/// Marks related MetricObservations as corrected when a habit completion is undone.
/// </summary>
public sealed class HabitUndoneEventHandler : INotificationHandler<HabitUndoneEvent>
{
    private readonly IHabitRepository _habitRepository;
    private readonly IMetricObservationRepository _observationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public HabitUndoneEventHandler(
        IHabitRepository habitRepository,
        IMetricObservationRepository observationRepository,
        IUnitOfWork unitOfWork)
    {
        _habitRepository = habitRepository;
        _observationRepository = observationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(HabitUndoneEvent notification, CancellationToken cancellationToken)
    {
        // Find all observations with this correlation ID and mark them as corrected
        var correlationId = $"HabitOccurrence:{notification.OccurrenceId}";
        var observations = await _observationRepository.GetByCorrelationIdAsync(correlationId, cancellationToken);

        foreach (var observation in observations)
        {
            // Create a correction with value 0 to negate the observation
            observation.CreateCorrection(0, "Habit completion undone");
        }

        // Update streak
        var habit = await _habitRepository.GetByIdAsync(notification.HabitId, cancellationToken);
        if (habit != null)
        {
            var streak = await _habitRepository.CalculateStreakAsync(notification.HabitId, cancellationToken);
            habit.UpdateStreak(streak);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

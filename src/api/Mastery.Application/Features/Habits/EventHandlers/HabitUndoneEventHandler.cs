using MediatR;
using Mastery.Domain.Entities.Habit;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Habits.EventHandlers;

/// <summary>
/// Marks related MetricObservations as corrected when a habit completion is undone.
/// Note: SaveChangesAsync is NOT called here - changes are committed by the parent transaction.
/// </summary>
public sealed class HabitUndoneEventHandler(
    IHabitRepository _habitRepository,
    IMetricObservationRepository _observationRepository)
    : INotificationHandler<HabitUndoneEvent>
{
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
    }
}

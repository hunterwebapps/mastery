using MediatR;
using Mastery.Domain.Entities.Habit;
using Mastery.Domain.Entities.Metrics;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Habits.EventHandlers;

/// <summary>
/// Creates MetricObservations when a habit is completed.
/// This is the critical integration point between Habits and the Metrics system.
/// Note: SaveChangesAsync is NOT called here - changes are committed by the parent transaction.
/// </summary>
public sealed class HabitCompletedEventHandler(
    IHabitRepository _habitRepository,
    IMetricObservationRepository _observationRepository)
    : INotificationHandler<HabitCompletedEvent>
{
    public async Task Handle(HabitCompletedEvent notification, CancellationToken cancellationToken)
    {
        // Get the habit with its metric bindings
        var habit = await _habitRepository.GetByIdWithDetailsAsync(notification.HabitId, cancellationToken);

        if (habit == null || habit.MetricBindings.Count == 0)
            return;

        // Create observations for each bound metric
        foreach (var binding in habit.MetricBindings)
        {
            var value = binding.GetContributionValue(notification.EnteredValue);

            var observation = MetricObservation.Create(
                metricDefinitionId: binding.MetricDefinitionId,
                userId: habit.UserId,
                observedAt: DateTime.UtcNow,
                observedOn: notification.CompletedOn,
                value: value,
                source: MetricSourceType.Habit,
                correlationId: $"HabitOccurrence:{notification.OccurrenceId}",
                note: notification.ModeUsed.HasValue
                    ? $"Completed as {notification.ModeUsed.Value}"
                    : null);

            await _observationRepository.AddAsync(observation, cancellationToken);
        }

        // Update streak (this could be moved to a separate handler or service)
        var streak = await _habitRepository.CalculateStreakAsync(notification.HabitId, cancellationToken);
        habit.UpdateStreak(streak);
    }
}

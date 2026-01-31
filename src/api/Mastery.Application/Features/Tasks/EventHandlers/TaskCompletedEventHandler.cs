using MediatR;
using Mastery.Domain.Entities.Metrics;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;
using TaskCompletedEvent = Mastery.Domain.Entities.Task.TaskCompletedEvent;

namespace Mastery.Application.Features.Tasks.EventHandlers;

/// <summary>
/// Creates MetricObservations when a task is completed.
/// This is the critical integration point between Tasks and the Metrics system.
/// Note: SaveChangesAsync is NOT called here - changes are committed by the parent transaction.
/// </summary>
public sealed class TaskCompletedEventHandler(
    ITaskRepository _taskRepository,
    IMetricObservationRepository _observationRepository)
    : INotificationHandler<TaskCompletedEvent>
{
    public async Task Handle(TaskCompletedEvent notification, CancellationToken cancellationToken)
    {
        // Get the task with its metric bindings
        var task = await _taskRepository.GetByIdWithDetailsAsync(notification.TaskId, cancellationToken);

        if (task == null || task.MetricBindings.Count == 0)
            return;

        // Create observations for each bound metric
        foreach (var binding in task.MetricBindings)
        {
            var value = binding.GetContributionValue(notification.ActualMinutes, notification.EnteredValue);

            var observation = MetricObservation.Create(
                metricDefinitionId: binding.MetricDefinitionId,
                userId: task.UserId,
                observedAt: DateTime.UtcNow,
                observedOn: notification.CompletedOn,
                value: value,
                source: MetricSourceType.Task,
                correlationId: $"Task:{notification.TaskId}",
                note: binding.Notes);

            await _observationRepository.AddAsync(observation, cancellationToken);
        }
    }
}

using MediatR;
using Mastery.Domain.Events;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Tasks.EventHandlers;

/// <summary>
/// Marks related MetricObservations as corrected when a task completion is undone.
/// </summary>
public sealed class TaskCompletionUndoneEventHandler : INotificationHandler<TaskCompletionUndoneEvent>
{
    private readonly IMetricObservationRepository _observationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TaskCompletionUndoneEventHandler(
        IMetricObservationRepository observationRepository,
        IUnitOfWork unitOfWork)
    {
        _observationRepository = observationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(TaskCompletionUndoneEvent notification, CancellationToken cancellationToken)
    {
        // Find all observations with this correlation ID and mark them as corrected
        var correlationId = $"Task:{notification.TaskId}";
        var observations = await _observationRepository.GetByCorrelationIdAsync(correlationId, cancellationToken);

        foreach (var observation in observations)
        {
            // Create a correction with value 0 to negate the observation
            observation.CreateCorrection(0, "Task completion undone");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

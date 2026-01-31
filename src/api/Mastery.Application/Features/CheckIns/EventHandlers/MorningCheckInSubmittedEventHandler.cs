using MediatR;
using Mastery.Domain.Entities.CheckIn;
using Mastery.Domain.Entities.Metrics;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.CheckIns.EventHandlers;

/// <summary>
/// Creates a MetricObservation for Energy when a morning check-in is submitted.
/// This bridges the check-in system into the metrics system for trend analysis.
/// Note: SaveChangesAsync is NOT called here - changes are committed by the parent transaction.
/// </summary>
public sealed class MorningCheckInSubmittedEventHandler(
    IMetricDefinitionRepository _metricDefinitionRepository,
    IMetricObservationRepository _observationRepository)
    : INotificationHandler<MorningCheckInSubmittedEvent>
{
    private const string EnergyMetricName = "Energy Level";

    public async Task Handle(MorningCheckInSubmittedEvent notification, CancellationToken cancellationToken)
    {
        // Find the user's Energy metric definition
        var energyMetric = await _metricDefinitionRepository.GetByUserIdAndNameAsync(
            notification.UserId, EnergyMetricName, cancellationToken);

        // If the user hasn't set up an Energy metric, skip observation creation
        if (energyMetric == null)
            return;

        var observation = MetricObservation.Create(
            metricDefinitionId: energyMetric.Id,
            userId: notification.UserId,
            observedAt: DateTime.UtcNow,
            observedOn: notification.CheckInDate,
            value: notification.EnergyLevel,
            source: MetricSourceType.CheckIn,
            correlationId: $"CheckIn:{notification.CheckInId}",
            note: $"Morning energy: {notification.EnergyLevel}/5");

        await _observationRepository.AddAsync(observation, cancellationToken);
    }
}

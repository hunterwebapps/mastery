using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Metrics.Commands.RecordObservation;

/// <summary>
/// Records a new observation for a metric.
/// </summary>
public sealed record RecordObservationCommand(
    Guid MetricDefinitionId,
    decimal Value,
    DateOnly? ObservedOn = null,
    string Source = "Manual",
    string? CorrelationId = null,
    string? Note = null) : ICommand<Guid>;

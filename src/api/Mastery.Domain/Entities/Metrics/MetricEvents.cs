using Mastery.Domain.Common;
using Mastery.Domain.Enums;

namespace Mastery.Domain.Entities.Metrics;

/// <summary>
/// Raised when a new metric definition is created.
/// </summary>
[SignalClassification(SignalPriority.Low, ProcessingWindowType.BatchWindow,
    Rationale = "Setup event - triggers metadata indexing")]
public sealed record MetricDefinitionCreatedEvent(
    Guid MetricDefinitionId,
    string UserId,
    string Name) : DomainEvent;

/// <summary>
/// Raised when a metric definition is updated.
/// </summary>
[SignalClassification(SignalPriority.Low, ProcessingWindowType.BatchWindow,
    Rationale = "Metadata update - triggers re-indexing")]
public sealed record MetricDefinitionUpdatedEvent(
    Guid MetricDefinitionId,
    string Name) : DomainEvent;

/// <summary>
/// Raised when a metric definition is archived.
/// </summary>
[SignalClassification(SignalPriority.Low, ProcessingWindowType.BatchWindow,
    Rationale = "Lifecycle change - removes from active tracking")]
public sealed record MetricDefinitionArchivedEvent(
    Guid MetricDefinitionId,
    string UserId) : DomainEvent;

/// <summary>
/// Raised when a metric observation is recorded.
/// </summary>
[SignalClassification(SignalPriority.Standard, ProcessingWindowType.BatchWindow,
    Rationale = "Behavioral signal - affects goal progress and recommendations")]
public sealed record MetricObservationRecordedEvent(
    Guid ObservationId,
    Guid MetricDefinitionId,
    string UserId,
    DateOnly ObservedOn,
    decimal Value,
    MetricSourceType Source) : DomainEvent;

/// <summary>
/// Raised when a metric observation is corrected.
/// </summary>
[NoSignal(Reason = "Internal correction - no signal needed")]
public sealed record MetricObservationCorrectedEvent(
    Guid NewObservationId,
    Guid OriginalObservationId,
    Guid MetricDefinitionId,
    string UserId,
    decimal OldValue,
    decimal NewValue) : DomainEvent;

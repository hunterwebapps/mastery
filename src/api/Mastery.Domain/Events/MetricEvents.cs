using Mastery.Domain.Common;
using Mastery.Domain.Enums;

namespace Mastery.Domain.Events;

/// <summary>
/// Raised when a new metric definition is created.
/// </summary>
public sealed record MetricDefinitionCreatedEvent(
    Guid MetricDefinitionId,
    string UserId,
    string Name) : DomainEvent;

/// <summary>
/// Raised when a metric definition is updated.
/// </summary>
public sealed record MetricDefinitionUpdatedEvent(
    Guid MetricDefinitionId,
    string Name) : DomainEvent;

/// <summary>
/// Raised when a metric definition is archived.
/// </summary>
public sealed record MetricDefinitionArchivedEvent(
    Guid MetricDefinitionId,
    string UserId) : DomainEvent;

/// <summary>
/// Raised when a metric observation is recorded.
/// </summary>
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
public sealed record MetricObservationCorrectedEvent(
    Guid NewObservationId,
    Guid OriginalObservationId,
    Guid MetricDefinitionId,
    string UserId,
    decimal OldValue,
    decimal NewValue) : DomainEvent;

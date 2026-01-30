using Mastery.Domain.Common;
using Mastery.Domain.Enums;

namespace Mastery.Domain.Entities.Goal;

/// <summary>
/// Raised when a new goal is created.
/// </summary>
[SignalClassification(SignalPriority.Low, ProcessingWindowType.BatchWindow,
    Rationale = "Setup event - triggers metadata indexing")]
public sealed record GoalCreatedEvent(
    Guid GoalId,
    string UserId,
    string Title) : DomainEvent;

/// <summary>
/// Raised when a goal's details are updated.
/// </summary>
[SignalClassification(SignalPriority.Low, ProcessingWindowType.BatchWindow,
    Rationale = "Metadata update - triggers re-indexing")]
public sealed record GoalUpdatedEvent(
    Guid GoalId,
    string ChangedSection) : DomainEvent;

/// <summary>
/// Raised when a goal's status changes.
/// </summary>
[SignalClassification(SignalPriority.Standard, ProcessingWindowType.BatchWindow,
    Rationale = "Behavioral signal - affects goal progress tracking")]
public sealed record GoalStatusChangedEvent(
    Guid GoalId,
    string UserId,
    GoalStatus NewStatus) : DomainEvent;

/// <summary>
/// Raised when a goal is completed.
/// </summary>
[SignalClassification(SignalPriority.Standard, ProcessingWindowType.BatchWindow,
    Rationale = "Major achievement - triggers celebration and review")]
public sealed record GoalCompletedEvent(
    Guid GoalId,
    string UserId,
    string? CompletionNotes) : DomainEvent;

/// <summary>
/// Raised when a goal's scoreboard is updated.
/// </summary>
[NoSignal(Reason = "Internal scoreboard update")]
public sealed record GoalScoreboardUpdatedEvent(
    Guid GoalId,
    string ChangeType,
    Guid? AffectedMetricDefinitionId) : DomainEvent;

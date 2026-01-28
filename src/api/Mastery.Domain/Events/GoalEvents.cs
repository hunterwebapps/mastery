using Mastery.Domain.Common;
using Mastery.Domain.Enums;

namespace Mastery.Domain.Events;

/// <summary>
/// Raised when a new goal is created.
/// </summary>
public sealed record GoalCreatedEvent(
    Guid GoalId,
    string UserId,
    string Title) : DomainEvent;

/// <summary>
/// Raised when a goal's details are updated.
/// </summary>
public sealed record GoalUpdatedEvent(
    Guid GoalId,
    string ChangedSection) : DomainEvent;

/// <summary>
/// Raised when a goal's status changes.
/// </summary>
public sealed record GoalStatusChangedEvent(
    Guid GoalId,
    string UserId,
    GoalStatus NewStatus) : DomainEvent;

/// <summary>
/// Raised when a goal is completed.
/// </summary>
public sealed record GoalCompletedEvent(
    Guid GoalId,
    string UserId,
    string? CompletionNotes) : DomainEvent;

/// <summary>
/// Raised when a goal's scoreboard is updated.
/// </summary>
public sealed record GoalScoreboardUpdatedEvent(
    Guid GoalId,
    string ChangeType,
    Guid? AffectedMetricDefinitionId) : DomainEvent;

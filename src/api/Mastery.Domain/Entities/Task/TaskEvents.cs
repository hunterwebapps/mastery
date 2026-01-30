using Mastery.Domain.Common;
using Mastery.Domain.Enums;
using TaskStatus = Mastery.Domain.Enums.TaskStatus;

namespace Mastery.Domain.Entities.Task;

/// <summary>
/// Raised when a new task is created.
/// </summary>
[SignalClassification(SignalPriority.Low, ProcessingWindowType.BatchWindow,
    Rationale = "Setup event - triggers metadata indexing")]
public sealed record TaskCreatedEvent(
    Guid TaskId,
    string UserId,
    string Title) : DomainEvent;

/// <summary>
/// Raised when a task's details are updated.
/// </summary>
[SignalClassification(SignalPriority.Low, ProcessingWindowType.BatchWindow,
    Rationale = "Metadata update - triggers re-indexing")]
public sealed record TaskUpdatedEvent(
    Guid TaskId,
    string ChangedSection) : DomainEvent;

/// <summary>
/// Raised when a task's status changes.
/// </summary>
[NoSignal(Reason = "Internal state transition")]
public sealed record TaskStatusChangedEvent(
    Guid TaskId,
    string UserId,
    TaskStatus NewStatus,
    TaskStatus OldStatus) : DomainEvent;

/// <summary>
/// Raised when a task is scheduled for a specific date.
/// </summary>
[NoSignal(Reason = "Internal scheduling event")]
public sealed record TaskScheduledEvent(
    Guid TaskId,
    string UserId,
    DateOnly ScheduledOn) : DomainEvent;

/// <summary>
/// Raised when a task is rescheduled.
/// Includes the reason for friction analysis.
/// </summary>
[SignalClassification(SignalPriority.Standard, ProcessingWindowType.BatchWindow,
    Rationale = "Behavioral signal - may trigger P0 via reschedule pattern")]
public sealed record TaskRescheduledEvent(
    Guid TaskId,
    string UserId,
    DateOnly OldDate,
    DateOnly NewDate,
    RescheduleReason? Reason) : DomainEvent;

/// <summary>
/// Raised when a task is completed.
/// This event triggers metric observation creation for bound metrics.
/// </summary>
[SignalClassification(SignalPriority.Standard, ProcessingWindowType.BatchWindow,
    Rationale = "Behavioral signal - affects capacity and recommendations")]
public sealed record TaskCompletedEvent(
    Guid TaskId,
    string UserId,
    DateOnly CompletedOn,
    int? ActualMinutes,
    decimal? EnteredValue) : DomainEvent;

/// <summary>
/// Raised when a task completion is undone.
/// This event triggers correction of related metric observations.
/// </summary>
[NoSignal(Reason = "Internal correction - no signal needed")]
public sealed record TaskCompletionUndoneEvent(
    Guid TaskId,
    string UserId) : DomainEvent;

/// <summary>
/// Raised when a task is cancelled.
/// Used for diagnostic signals.
/// </summary>
[NoSignal(Reason = "Internal lifecycle event")]
public sealed record TaskCancelledEvent(
    Guid TaskId,
    string UserId) : DomainEvent;

/// <summary>
/// Raised when a task is archived.
/// </summary>
[SignalClassification(SignalPriority.Low, ProcessingWindowType.BatchWindow,
    Rationale = "Lifecycle change - removes from active planning")]
public sealed record TaskArchivedEvent(
    Guid TaskId,
    string UserId) : DomainEvent;

/// <summary>
/// Raised when a dependency is added to a task.
/// </summary>
[NoSignal(Reason = "Internal graph update")]
public sealed record TaskDependencyAddedEvent(
    Guid TaskId,
    Guid DependencyTaskId) : DomainEvent;

/// <summary>
/// Raised when a dependency is removed from a task.
/// </summary>
[NoSignal(Reason = "Internal graph update")]
public sealed record TaskDependencyRemovedEvent(
    Guid TaskId,
    Guid DependencyTaskId) : DomainEvent;

using Mastery.Domain.Common;
using Mastery.Domain.Enums;
using TaskStatus = Mastery.Domain.Enums.TaskStatus;

namespace Mastery.Domain.Events;

/// <summary>
/// Raised when a new task is created.
/// </summary>
public sealed record TaskCreatedEvent(
    Guid TaskId,
    string UserId,
    string Title) : DomainEvent;

/// <summary>
/// Raised when a task's details are updated.
/// </summary>
public sealed record TaskUpdatedEvent(
    Guid TaskId,
    string ChangedSection) : DomainEvent;

/// <summary>
/// Raised when a task's status changes.
/// </summary>
public sealed record TaskStatusChangedEvent(
    Guid TaskId,
    string UserId,
    TaskStatus NewStatus,
    TaskStatus OldStatus) : DomainEvent;

/// <summary>
/// Raised when a task is scheduled for a specific date.
/// </summary>
public sealed record TaskScheduledEvent(
    Guid TaskId,
    string UserId,
    DateOnly ScheduledOn) : DomainEvent;

/// <summary>
/// Raised when a task is rescheduled.
/// Includes the reason for friction analysis.
/// </summary>
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
public sealed record TaskCompletionUndoneEvent(
    Guid TaskId,
    string UserId) : DomainEvent;

/// <summary>
/// Raised when a task is cancelled.
/// Used for diagnostic signals.
/// </summary>
public sealed record TaskCancelledEvent(
    Guid TaskId,
    string UserId) : DomainEvent;

/// <summary>
/// Raised when a task is archived.
/// </summary>
public sealed record TaskArchivedEvent(
    Guid TaskId,
    string UserId) : DomainEvent;

/// <summary>
/// Raised when a dependency is added to a task.
/// </summary>
public sealed record TaskDependencyAddedEvent(
    Guid TaskId,
    Guid DependencyTaskId) : DomainEvent;

/// <summary>
/// Raised when a dependency is removed from a task.
/// </summary>
public sealed record TaskDependencyRemovedEvent(
    Guid TaskId,
    Guid DependencyTaskId) : DomainEvent;

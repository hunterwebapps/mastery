using Mastery.Domain.Common;
using Mastery.Domain.Enums;

namespace Mastery.Domain.Events;

/// <summary>
/// Raised when a new habit is created.
/// </summary>
public sealed record HabitCreatedEvent(
    Guid HabitId,
    string UserId,
    string Title) : DomainEvent;

/// <summary>
/// Raised when a habit's details are updated.
/// </summary>
public sealed record HabitUpdatedEvent(
    Guid HabitId,
    string ChangedSection) : DomainEvent;

/// <summary>
/// Raised when a habit's status changes.
/// </summary>
public sealed record HabitStatusChangedEvent(
    Guid HabitId,
    string UserId,
    HabitStatus NewStatus) : DomainEvent;

/// <summary>
/// Raised when a habit is archived.
/// </summary>
public sealed record HabitArchivedEvent(
    Guid HabitId,
    string UserId) : DomainEvent;

/// <summary>
/// Raised when a habit occurrence is completed.
/// This event triggers metric observation creation.
/// </summary>
public sealed record HabitCompletedEvent(
    Guid OccurrenceId,
    Guid HabitId,
    string UserId,
    DateOnly CompletedOn,
    HabitMode? ModeUsed,
    decimal? EnteredValue) : DomainEvent;

/// <summary>
/// Raised when a habit occurrence is undone.
/// This event triggers correction of related metric observations.
/// </summary>
public sealed record HabitUndoneEvent(
    Guid OccurrenceId,
    Guid HabitId,
    DateOnly ScheduledOn) : DomainEvent;

/// <summary>
/// Raised when a habit occurrence is skipped.
/// </summary>
public sealed record HabitSkippedEvent(
    Guid OccurrenceId,
    Guid HabitId,
    DateOnly ScheduledOn,
    string? Reason) : DomainEvent;

/// <summary>
/// Raised when a habit occurrence is marked as missed.
/// Used for friction analysis.
/// </summary>
public sealed record HabitMissedEvent(
    Guid OccurrenceId,
    Guid HabitId,
    DateOnly ScheduledOn,
    MissReason Reason) : DomainEvent;

/// <summary>
/// Raised when a streak milestone is reached.
/// Triggers celebration notifications.
/// </summary>
public sealed record HabitStreakMilestoneEvent(
    Guid HabitId,
    string UserId,
    int StreakCount,
    string MilestoneType) : DomainEvent;

/// <summary>
/// Raised when the system suggests a different mode for a habit.
/// Used for intelligent mode scaling based on capacity.
/// </summary>
public sealed record HabitModeSuggestedEvent(
    Guid HabitId,
    string UserId,
    HabitMode SuggestedMode,
    string Reason) : DomainEvent;

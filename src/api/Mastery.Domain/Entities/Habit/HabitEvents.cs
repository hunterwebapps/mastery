using Mastery.Domain.Common;
using Mastery.Domain.Enums;

namespace Mastery.Domain.Entities.Habit;

/// <summary>
/// Raised when a new habit is created.
/// </summary>
[SignalClassification(SignalPriority.Low, ProcessingWindowType.BatchWindow,
    Rationale = "Setup event - triggers metadata indexing")]
public sealed record HabitCreatedEvent(
    Guid HabitId,
    string UserId,
    string Title) : DomainEvent;

/// <summary>
/// Raised when a habit's details are updated.
/// </summary>
[SignalClassification(SignalPriority.Low, ProcessingWindowType.BatchWindow,
    Rationale = "Metadata update - triggers re-indexing")]
public sealed record HabitUpdatedEvent(
    Guid HabitId,
    string ChangedSection) : DomainEvent;

/// <summary>
/// Raised when a habit's status changes.
/// </summary>
[SignalClassification(SignalPriority.Low, ProcessingWindowType.BatchWindow,
    Rationale = "Lifecycle change - may affect planning")]
public sealed record HabitStatusChangedEvent(
    Guid HabitId,
    string UserId,
    HabitStatus NewStatus) : DomainEvent;

/// <summary>
/// Raised when a habit is archived.
/// </summary>
[SignalClassification(SignalPriority.Low, ProcessingWindowType.BatchWindow,
    Rationale = "Lifecycle change - removes from active planning")]
public sealed record HabitArchivedEvent(
    Guid HabitId,
    string UserId) : DomainEvent;

/// <summary>
/// Raised when a habit occurrence is completed.
/// This event triggers metric observation creation.
/// </summary>
[SignalClassification(SignalPriority.Standard, ProcessingWindowType.BatchWindow,
    Rationale = "Behavioral signal - affects adherence and recommendations")]
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
[NoSignal(Reason = "Internal correction - no signal needed")]
public sealed record HabitUndoneEvent(
    Guid OccurrenceId,
    Guid HabitId,
    DateOnly ScheduledOn) : DomainEvent;

/// <summary>
/// Raised when a habit occurrence is skipped.
/// </summary>
[SignalClassification(SignalPriority.Standard, ProcessingWindowType.BatchWindow,
    Rationale = "Behavioral signal - may indicate friction")]
public sealed record HabitSkippedEvent(
    Guid OccurrenceId,
    Guid HabitId,
    DateOnly ScheduledOn,
    string? Reason) : DomainEvent;

/// <summary>
/// Raised when a habit occurrence is marked as missed.
/// Used for friction analysis.
/// </summary>
[SignalClassification(SignalPriority.Standard, ProcessingWindowType.BatchWindow,
    Rationale = "May trigger P0 via adherence detection")]
public sealed record HabitMissedEvent(
    Guid OccurrenceId,
    Guid HabitId,
    DateOnly ScheduledOn,
    MissReason Reason) : DomainEvent;

/// <summary>
/// Raised when a habit occurrence is rescheduled to a different date.
/// Critical for friction/rescheduling pattern detection.
/// </summary>
[SignalClassification(SignalPriority.Standard, ProcessingWindowType.BatchWindow,
    Rationale = "Rescheduling indicates friction - used by ReschedulePatternDetection")]
public sealed record HabitOccurrenceRescheduledEvent(
    Guid OccurrenceId,
    Guid HabitId,
    string UserId,
    DateOnly OriginalDate,
    DateOnly NewDate) : DomainEvent;

/// <summary>
/// Raised when a streak milestone is reached.
/// Triggers celebration notifications.
/// </summary>
[SignalClassification(SignalPriority.Standard, ProcessingWindowType.BatchWindow,
    Rationale = "Positive reinforcement opportunity")]
public sealed record HabitStreakMilestoneEvent(
    Guid HabitId,
    string UserId,
    int StreakCount,
    string MilestoneType) : DomainEvent;

/// <summary>
/// Raised when the system suggests a different mode for a habit.
/// Used for intelligent mode scaling based on capacity.
/// </summary>
[NoSignal(Reason = "Internal suggestion event")]
public sealed record HabitModeSuggestedEvent(
    Guid HabitId,
    string UserId,
    HabitMode SuggestedMode,
    string Reason) : DomainEvent;

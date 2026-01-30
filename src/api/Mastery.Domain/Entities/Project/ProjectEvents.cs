using Mastery.Domain.Common;
using Mastery.Domain.Enums;

namespace Mastery.Domain.Entities.Project;

/// <summary>
/// Raised when a new project is created.
/// </summary>
[SignalClassification(SignalPriority.Low, ProcessingWindowType.BatchWindow,
    Rationale = "Setup event - triggers metadata indexing")]
public sealed record ProjectCreatedEvent(
    Guid ProjectId,
    string UserId,
    string Title) : DomainEvent;

/// <summary>
/// Raised when a project's details are updated.
/// </summary>
[SignalClassification(SignalPriority.Low, ProcessingWindowType.BatchWindow,
    Rationale = "Metadata update - triggers re-indexing")]
public sealed record ProjectUpdatedEvent(
    Guid ProjectId,
    string ChangedSection) : DomainEvent;

/// <summary>
/// Raised when a project's status changes.
/// </summary>
[SignalClassification(SignalPriority.Standard, ProcessingWindowType.BatchWindow,
    Rationale = "Behavioral signal - affects project progress tracking")]
public sealed record ProjectStatusChangedEvent(
    Guid ProjectId,
    string UserId,
    ProjectStatus NewStatus,
    ProjectStatus OldStatus) : DomainEvent;

/// <summary>
/// Raised when the project's next action is set or cleared.
/// </summary>
[NoSignal(Reason = "Internal planning update")]
public sealed record ProjectNextActionSetEvent(
    Guid ProjectId,
    string UserId,
    Guid? NewTaskId,
    Guid? OldTaskId) : DomainEvent;

/// <summary>
/// Raised when a project is completed.
/// </summary>
[SignalClassification(SignalPriority.Standard, ProcessingWindowType.BatchWindow,
    Rationale = "Major achievement - triggers celebration and review")]
public sealed record ProjectCompletedEvent(
    Guid ProjectId,
    string UserId,
    string? OutcomeNotes) : DomainEvent;

/// <summary>
/// Raised when a milestone is added to a project.
/// </summary>
[NoSignal(Reason = "Internal planning update")]
public sealed record MilestoneAddedEvent(
    Guid ProjectId,
    Guid MilestoneId,
    string Title) : DomainEvent;

/// <summary>
/// Raised when a milestone is completed.
/// Triggers celebration for progress tracking.
/// </summary>
[SignalClassification(SignalPriority.Standard, ProcessingWindowType.BatchWindow,
    Rationale = "Progress milestone - triggers celebration")]
public sealed record MilestoneCompletedEvent(
    Guid ProjectId,
    Guid MilestoneId,
    string Title) : DomainEvent;

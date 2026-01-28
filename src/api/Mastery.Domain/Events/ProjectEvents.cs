using Mastery.Domain.Common;
using Mastery.Domain.Enums;

namespace Mastery.Domain.Events;

/// <summary>
/// Raised when a new project is created.
/// </summary>
public sealed record ProjectCreatedEvent(
    Guid ProjectId,
    string UserId,
    string Title) : DomainEvent;

/// <summary>
/// Raised when a project's details are updated.
/// </summary>
public sealed record ProjectUpdatedEvent(
    Guid ProjectId,
    string ChangedSection) : DomainEvent;

/// <summary>
/// Raised when a project's status changes.
/// </summary>
public sealed record ProjectStatusChangedEvent(
    Guid ProjectId,
    string UserId,
    ProjectStatus NewStatus,
    ProjectStatus OldStatus) : DomainEvent;

/// <summary>
/// Raised when the project's next action is set or cleared.
/// </summary>
public sealed record ProjectNextActionSetEvent(
    Guid ProjectId,
    string UserId,
    Guid? NewTaskId,
    Guid? OldTaskId) : DomainEvent;

/// <summary>
/// Raised when a project is completed.
/// </summary>
public sealed record ProjectCompletedEvent(
    Guid ProjectId,
    string UserId,
    string? OutcomeNotes) : DomainEvent;

/// <summary>
/// Raised when a milestone is added to a project.
/// </summary>
public sealed record MilestoneAddedEvent(
    Guid ProjectId,
    Guid MilestoneId,
    string Title) : DomainEvent;

/// <summary>
/// Raised when a milestone is completed.
/// Triggers celebration for progress tracking.
/// </summary>
public sealed record MilestoneCompletedEvent(
    Guid ProjectId,
    Guid MilestoneId,
    string Title) : DomainEvent;

using Mastery.Domain.Common;
using Mastery.Domain.Enums;

namespace Mastery.Domain.Entities.Experiment;

/// <summary>
/// Raised when a new experiment is created.
/// </summary>
[SignalClassification(SignalPriority.Low, ProcessingWindowType.BatchWindow,
    Rationale = "Setup event - triggers metadata indexing")]
public sealed record ExperimentCreatedEvent(
    Guid ExperimentId,
    string UserId,
    string Title) : DomainEvent;

/// <summary>
/// Raised when a draft experiment's details are updated.
/// </summary>
[SignalClassification(SignalPriority.Low, ProcessingWindowType.BatchWindow,
    Rationale = "Metadata update - triggers re-indexing")]
public sealed record ExperimentUpdatedEvent(
    Guid ExperimentId,
    string UserId) : DomainEvent;

/// <summary>
/// Raised when an experiment is started (Draft -> Active).
/// </summary>
[SignalClassification(SignalPriority.Standard, ProcessingWindowType.BatchWindow,
    Rationale = "Behavioral signal - experiment tracking begins")]
public sealed record ExperimentStartedEvent(
    Guid ExperimentId,
    string UserId) : DomainEvent;

/// <summary>
/// Raised when an experiment is paused (Active -> Paused).
/// </summary>
[NoSignal(Reason = "Internal state transition")]
public sealed record ExperimentPausedEvent(
    Guid ExperimentId,
    string UserId) : DomainEvent;

/// <summary>
/// Raised when a paused experiment is resumed (Paused -> Active).
/// </summary>
[NoSignal(Reason = "Internal state transition")]
public sealed record ExperimentResumedEvent(
    Guid ExperimentId,
    string UserId) : DomainEvent;

/// <summary>
/// Raised when an experiment is completed with a result.
/// </summary>
[SignalClassification(SignalPriority.Standard, ProcessingWindowType.BatchWindow,
    Rationale = "Experiment outcome - learning engine input")]
public sealed record ExperimentCompletedEvent(
    Guid ExperimentId,
    string UserId,
    ExperimentOutcome Outcome) : DomainEvent;

/// <summary>
/// Raised when an experiment is abandoned before completion.
/// </summary>
[SignalClassification(SignalPriority.Standard, ProcessingWindowType.BatchWindow,
    Rationale = "Experiment abandoned - may indicate friction")]
public sealed record ExperimentAbandonedEvent(
    Guid ExperimentId,
    string UserId,
    string? Reason) : DomainEvent;

/// <summary>
/// Raised when an experiment is archived.
/// </summary>
[NoSignal(Reason = "Internal lifecycle event - no signal needed")]
public sealed record ExperimentArchivedEvent(
    Guid ExperimentId,
    string UserId) : DomainEvent;

/// <summary>
/// Raised when a note is added to an experiment.
/// </summary>
[SignalClassification(SignalPriority.Low, ProcessingWindowType.BatchWindow,
    Rationale = "Note content may be useful for experiment analysis")]
public sealed record ExperimentNoteAddedEvent(
    Guid ExperimentId,
    Guid NoteId,
    string UserId) : DomainEvent;

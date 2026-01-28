using Mastery.Domain.Common;
using Mastery.Domain.Enums;

namespace Mastery.Domain.Events;

/// <summary>
/// Raised when a new experiment is created.
/// </summary>
public sealed record ExperimentCreatedEvent(
    Guid ExperimentId,
    string UserId,
    string Title) : DomainEvent;

/// <summary>
/// Raised when an experiment is started (Draft → Active).
/// </summary>
public sealed record ExperimentStartedEvent(
    Guid ExperimentId,
    string UserId) : DomainEvent;

/// <summary>
/// Raised when an experiment is paused (Active → Paused).
/// </summary>
public sealed record ExperimentPausedEvent(
    Guid ExperimentId,
    string UserId) : DomainEvent;

/// <summary>
/// Raised when a paused experiment is resumed (Paused → Active).
/// </summary>
public sealed record ExperimentResumedEvent(
    Guid ExperimentId,
    string UserId) : DomainEvent;

/// <summary>
/// Raised when an experiment is completed with a result.
/// </summary>
public sealed record ExperimentCompletedEvent(
    Guid ExperimentId,
    string UserId,
    ExperimentOutcome Outcome) : DomainEvent;

/// <summary>
/// Raised when an experiment is abandoned before completion.
/// </summary>
public sealed record ExperimentAbandonedEvent(
    Guid ExperimentId,
    string UserId,
    string? Reason) : DomainEvent;

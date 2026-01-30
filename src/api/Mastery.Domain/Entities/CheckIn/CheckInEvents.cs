using Mastery.Domain.Common;
using Mastery.Domain.Enums;

namespace Mastery.Domain.Entities.CheckIn;

/// <summary>
/// Raised when a morning check-in is submitted.
/// Triggers metric observation creation for energy and capacity signals.
/// </summary>
[SignalClassification(SignalPriority.WindowAligned, ProcessingWindowType.MorningWindow,
    Rationale = "User active - ideal for morning recommendations")]
public sealed record MorningCheckInSubmittedEvent(
    Guid CheckInId,
    string UserId,
    DateOnly CheckInDate,
    int EnergyLevel,
    HabitMode SelectedMode) : DomainEvent;

/// <summary>
/// Raised when an evening check-in is submitted.
/// Triggers adherence projection updates and diagnostic signals.
/// </summary>
[SignalClassification(SignalPriority.WindowAligned, ProcessingWindowType.EveningWindow,
    Rationale = "User reflecting - ideal for evening coaching")]
public sealed record EveningCheckInSubmittedEvent(
    Guid CheckInId,
    string UserId,
    DateOnly CheckInDate,
    bool? Top1Completed) : DomainEvent;

/// <summary>
/// Raised when a check-in is updated after initial submission.
/// </summary>
[SignalClassification(SignalPriority.Low, ProcessingWindowType.BatchWindow,
    Rationale = "Metadata update - triggers re-indexing")]
public sealed record CheckInUpdatedEvent(
    Guid CheckInId,
    string ChangedSection) : DomainEvent;

/// <summary>
/// Raised when a user explicitly skips a check-in.
/// </summary>
[SignalClassification(SignalPriority.WindowAligned, ProcessingWindowType.BatchWindow,
    Rationale = "May indicate disengagement")]
public sealed record CheckInSkippedEvent(
    Guid CheckInId,
    string UserId,
    DateOnly CheckInDate,
    CheckInType Type) : DomainEvent;

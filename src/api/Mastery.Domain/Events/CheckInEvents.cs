using Mastery.Domain.Common;
using Mastery.Domain.Enums;

namespace Mastery.Domain.Events;

/// <summary>
/// Raised when a morning check-in is submitted.
/// Triggers metric observation creation for energy and capacity signals.
/// </summary>
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
public sealed record EveningCheckInSubmittedEvent(
    Guid CheckInId,
    string UserId,
    DateOnly CheckInDate,
    bool? Top1Completed) : DomainEvent;

/// <summary>
/// Raised when a check-in is updated after initial submission.
/// </summary>
public sealed record CheckInUpdatedEvent(
    Guid CheckInId,
    string ChangedSection) : DomainEvent;

/// <summary>
/// Raised when a user explicitly skips a check-in.
/// </summary>
public sealed record CheckInSkippedEvent(
    Guid CheckInId,
    string UserId,
    DateOnly CheckInDate,
    CheckInType Type) : DomainEvent;

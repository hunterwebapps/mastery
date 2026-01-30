using Mastery.Domain.Common;
using Mastery.Domain.Enums;

namespace Mastery.Domain.Entities.UserProfile;

/// <summary>
/// Raised when a new season is created.
/// </summary>
[SignalClassification(SignalPriority.Low, ProcessingWindowType.BatchWindow,
    Rationale = "Setup event - triggers metadata indexing")]
public sealed record SeasonCreatedEvent(
    Guid SeasonId,
    string UserId,
    SeasonType Type,
    DateOnly StartDate) : DomainEvent;

/// <summary>
/// Raised when a season is set as the user's current season.
/// </summary>
[SignalClassification(SignalPriority.Standard, ProcessingWindowType.BatchWindow)]
public sealed record SeasonActivatedEvent(
    Guid SeasonId,
    string UserId,
    Guid? PreviousSeasonId) : DomainEvent;

/// <summary>
/// Raised when a season is ended.
/// </summary>
[SignalClassification(SignalPriority.Low, ProcessingWindowType.BatchWindow,
    Rationale = "Lifecycle change - may trigger review")]
public sealed record SeasonEndedEvent(
    Guid SeasonId,
    string UserId,
    DateOnly EndDate,
    string? Outcome) : DomainEvent;

/// <summary>
/// Raised when the current season is cleared without setting a new one.
/// </summary>
[NoSignal(Reason = "Internal state transition")]
public sealed record SeasonClearedEvent(
    Guid ProfileId,
    string UserId,
    Guid? PreviousSeasonId) : DomainEvent;

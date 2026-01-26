using Mastery.Domain.Common;
using Mastery.Domain.Entities;

namespace Mastery.Domain.Events;

/// <summary>
/// Raised when a new season is created.
/// </summary>
public sealed record SeasonCreatedEvent(
    Guid SeasonId,
    string UserId,
    SeasonType Type,
    DateOnly StartDate) : DomainEvent;

/// <summary>
/// Raised when a season is set as the user's current season.
/// </summary>
public sealed record SeasonActivatedEvent(
    Guid SeasonId,
    string UserId,
    Guid? PreviousSeasonId) : DomainEvent;

/// <summary>
/// Raised when a season is ended.
/// </summary>
public sealed record SeasonEndedEvent(
    Guid SeasonId,
    string UserId,
    DateOnly EndDate,
    string? Outcome) : DomainEvent;

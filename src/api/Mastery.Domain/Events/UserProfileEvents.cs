using Mastery.Domain.Common;

namespace Mastery.Domain.Events;

/// <summary>
/// Raised when a user profile is created (onboarding complete).
/// </summary>
public sealed record UserProfileCreatedEvent(
    Guid ProfileId,
    string UserId) : DomainEvent;

/// <summary>
/// Raised when a user profile section is updated.
/// </summary>
public sealed record UserProfileUpdatedEvent(
    Guid ProfileId,
    string ChangedSection) : DomainEvent;

/// <summary>
/// Raised when user preferences are updated.
/// </summary>
public sealed record PreferencesUpdatedEvent(
    Guid ProfileId) : DomainEvent;

/// <summary>
/// Raised when user constraints are updated.
/// </summary>
public sealed record ConstraintsUpdatedEvent(
    Guid ProfileId) : DomainEvent;

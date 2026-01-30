using Mastery.Domain.Common;
using Mastery.Domain.Enums;

namespace Mastery.Domain.Entities.UserProfile;

/// <summary>
/// Raised when a user profile is created (onboarding complete).
/// </summary>
[SignalClassification(SignalPriority.Low, ProcessingWindowType.BatchWindow,
    Rationale = "Setup event - triggers metadata indexing")]
public sealed record UserProfileCreatedEvent(
    Guid ProfileId,
    string UserId) : DomainEvent;

/// <summary>
/// Raised when a user profile section is updated.
/// </summary>
[SignalClassification(SignalPriority.Low, ProcessingWindowType.BatchWindow,
    Rationale = "Profile update - may affect capacity and preferences")]
public sealed record UserProfileUpdatedEvent(
    Guid ProfileId,
    string ChangedSection) : DomainEvent;

/// <summary>
/// Raised when user preferences are updated.
/// </summary>
[NoSignal(Reason = "Internal preference update")]
public sealed record PreferencesUpdatedEvent(
    Guid ProfileId) : DomainEvent;

/// <summary>
/// Raised when user constraints are updated.
/// </summary>
[NoSignal(Reason = "Internal constraint update")]
public sealed record ConstraintsUpdatedEvent(
    Guid ProfileId) : DomainEvent;

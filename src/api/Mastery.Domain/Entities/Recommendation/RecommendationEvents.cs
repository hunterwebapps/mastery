using Mastery.Domain.Common;
using Mastery.Domain.Enums;

namespace Mastery.Domain.Entities.Recommendation;

/// <summary>
/// Raised when recommendations are generated for a user.
/// </summary>
[NoSignal(Reason = "System output event - no signal needed")]
public sealed record RecommendationsGeneratedEvent(
    string UserId,
    RecommendationContext Context,
    int Count) : DomainEvent;

/// <summary>
/// Raised when a user accepts a recommendation.
/// </summary>
[NoSignal(Reason = "Feedback event - processed separately")]
public sealed record RecommendationAcceptedEvent(
    Guid RecommendationId,
    string UserId,
    RecommendationType Type) : DomainEvent;

/// <summary>
/// Raised when a user dismisses a recommendation.
/// </summary>
[NoSignal(Reason = "Feedback event - processed separately")]
public sealed record RecommendationDismissedEvent(
    Guid RecommendationId,
    string UserId,
    RecommendationType Type,
    string? Reason) : DomainEvent;

/// <summary>
/// Raised when a user snoozes a recommendation.
/// </summary>
[NoSignal(Reason = "Feedback event - processed separately")]
public sealed record RecommendationSnoozedEvent(
    Guid RecommendationId,
    string UserId) : DomainEvent;

/// <summary>
/// Raised when a diagnostic signal is detected by the rules engine.
/// </summary>
[NoSignal(Reason = "Internal diagnostic event")]
public sealed record DiagnosticSignalDetectedEvent(
    Guid SignalId,
    string UserId,
    SignalType Type,
    int Severity) : DomainEvent;

using Mastery.Domain.Common;
using Mastery.Domain.Enums;

namespace Mastery.Domain.Events;

public sealed record RecommendationsGeneratedEvent(
    string UserId,
    RecommendationContext Context,
    int Count) : DomainEvent;

public sealed record RecommendationAcceptedEvent(
    Guid RecommendationId,
    string UserId,
    RecommendationType Type) : DomainEvent;

public sealed record RecommendationDismissedEvent(
    Guid RecommendationId,
    string UserId,
    RecommendationType Type,
    string? Reason) : DomainEvent;

public sealed record RecommendationSnoozedEvent(
    Guid RecommendationId,
    string UserId) : DomainEvent;

public sealed record DiagnosticSignalDetectedEvent(
    Guid SignalId,
    string UserId,
    SignalType Type,
    int Severity) : DomainEvent;

using Mastery.Domain.Enums;

namespace Mastery.Infrastructure.Messaging.Events;

/// <summary>
/// Event published after embedding generation when a signal needs to be processed.
/// Routed to the appropriate topic based on priority (urgent/window/batch).
/// </summary>
public sealed record SignalRoutedEvent
{
    /// <summary>
    /// Unique identifier for this event (for idempotency).
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The user ID this signal belongs to.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// The type of domain event that triggered this signal (e.g., "HabitCompletedEvent").
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// Priority level determining processing urgency.
    /// </summary>
    public SignalPriority Priority { get; init; }

    /// <summary>
    /// The type of processing window for this signal.
    /// </summary>
    public ProcessingWindowType WindowType { get; init; }

    /// <summary>
    /// The entity type that triggered this signal (e.g., "Goal", "Habit", "CheckIn").
    /// </summary>
    public string? TargetEntityType { get; init; }

    /// <summary>
    /// The ID of the entity that triggered this signal.
    /// </summary>
    public Guid? TargetEntityId { get; init; }

    /// <summary>
    /// When this event was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// The scheduled window start time for non-immediate signals.
    /// </summary>
    public DateTime? ScheduledWindowStart { get; init; }

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; init; }
}

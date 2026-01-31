namespace Mastery.Infrastructure.Messaging.Events;

/// <summary>
/// Event published when an entity changes and needs embedding generation.
/// Published to the embeddings-pending topic.
/// </summary>
public sealed record EntityChangedEvent
{
    /// <summary>
    /// Unique identifier for this event (for idempotency).
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The type of entity that changed (e.g., "Goal", "Habit", "Task").
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// The unique identifier of the changed entity.
    /// </summary>
    public required Guid EntityId { get; init; }

    /// <summary>
    /// The type of operation: "Created", "Updated", or "Deleted".
    /// </summary>
    public required string Operation { get; init; }

    /// <summary>
    /// The user ID associated with the entity (for user-scoped processing).
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// The domain event type that triggered this change (e.g., "HabitCompletedEvent").
    /// Used for signal classification.
    /// </summary>
    public string[] DomainEventTypes { get; init; } = Array.Empty<string>();

    /// <summary>
    /// When this event was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; init; }
}

namespace Mastery.Infrastructure.Messaging.Events;

/// <summary>
/// Batch event containing multiple entity changes for efficient processing.
/// Published to the embeddings-pending queue as a single message.
/// </summary>
public sealed record EntityChangedBatchEvent
{
    /// <summary>
    /// Unique identifier for this batch (for idempotency).
    /// </summary>
    public Guid BatchId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The individual entity changes in this batch.
    /// </summary>
    public List<EntityChangedEvent> Events { get; init; } = new();

    /// <summary>
    /// When this batch was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; init; }
}

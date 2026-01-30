namespace Mastery.Infrastructure.Messaging.Events;

/// <summary>
/// Batch event containing multiple signals for efficient processing.
/// Published to signal queues as a single message per user.
/// </summary>
public sealed record SignalRoutedBatchEvent
{
    /// <summary>
    /// Unique identifier for this batch (for idempotency).
    /// </summary>
    public Guid BatchId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The user ID this batch belongs to.
    /// All signals in a batch must be for the same user.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// The individual signals in this batch.
    /// </summary>
    public required IReadOnlyList<SignalRoutedEvent> Signals { get; init; }

    /// <summary>
    /// When this batch was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; init; }
}

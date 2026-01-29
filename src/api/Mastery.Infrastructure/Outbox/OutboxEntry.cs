namespace Mastery.Infrastructure.Outbox;

/// <summary>
/// Represents an entity change that needs to be processed for downstream embedding generation.
/// Uses a query-at-process-time strategy: no payload is stored, entities are queried when processed.
/// This enables deduplication and ensures current state is always used.
/// </summary>
public sealed class OutboxEntry
{
    /// <summary>
    /// Auto-incrementing primary key (BIGINT IDENTITY).
    /// </summary>
    public long Id { get; private set; }

    /// <summary>
    /// The type of entity that changed (e.g., "Goal", "Habit", "Task").
    /// </summary>
    public string EntityType { get; private set; } = null!;

    /// <summary>
    /// The unique identifier of the changed entity.
    /// </summary>
    public Guid EntityId { get; private set; }

    /// <summary>
    /// The type of operation: "Created", "Updated", or "Deleted".
    /// </summary>
    public string Operation { get; private set; } = null!;

    /// <summary>
    /// The user ID associated with the entity (for user-scoped processing).
    /// </summary>
    public string? UserId { get; private set; }

    /// <summary>
    /// When this entry was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When this entry was successfully processed.
    /// </summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>
    /// Lease expiration time. If set and in the future, another worker has this entry.
    /// </summary>
    public DateTime? LeasedUntil { get; private set; }

    /// <summary>
    /// Identifier of the worker that currently holds the lease.
    /// </summary>
    public string? LeaseHolder { get; private set; }

    /// <summary>
    /// Number of times processing has been attempted.
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// Error message from the last failed processing attempt.
    /// </summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// Current status of this outbox entry.
    /// </summary>
    public OutboxEntryStatus Status { get; private set; }

    // Private constructor for EF Core
    private OutboxEntry() { }

    /// <summary>
    /// Creates a new outbox entry for an entity change.
    /// </summary>
    public static OutboxEntry Create(
        string entityType,
        Guid entityId,
        string operation,
        string? userId,
        DateTime createdAt)
    {
        return new OutboxEntry
        {
            EntityType = entityType,
            EntityId = entityId,
            Operation = operation,
            UserId = userId,
            CreatedAt = createdAt,
            Status = OutboxEntryStatus.Pending,
            RetryCount = 0
        };
    }

    /// <summary>
    /// Attempts to acquire a lease on this entry.
    /// Returns true if the lease was acquired, false if already leased.
    /// </summary>
    public bool TryAcquireLease(string leaseHolder, DateTime leasedUntil, DateTime now)
    {
        // Can only lease if Pending, or if Processing with expired lease
        if (Status == OutboxEntryStatus.Pending ||
            (Status == OutboxEntryStatus.Processing && LeasedUntil.HasValue && LeasedUntil.Value < now))
        {
            LeaseHolder = leaseHolder;
            LeasedUntil = leasedUntil;
            Status = OutboxEntryStatus.Processing;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Marks this entry as successfully processed.
    /// </summary>
    public void MarkProcessed(DateTime processedAt)
    {
        ProcessedAt = processedAt;
        Status = OutboxEntryStatus.Processed;
        LeasedUntil = null;
        LeaseHolder = null;
    }

    /// <summary>
    /// Marks this entry as failed, incrementing the retry count.
    /// If max retries exceeded, status becomes Failed; otherwise returns to Pending.
    /// </summary>
    public void MarkFailed(string error, int maxRetries)
    {
        RetryCount++;
        LastError = error;
        LeasedUntil = null;
        LeaseHolder = null;

        Status = RetryCount >= maxRetries
            ? OutboxEntryStatus.Failed
            : OutboxEntryStatus.Pending;
    }

    /// <summary>
    /// Releases the lease without marking as processed or failed.
    /// Used for graceful shutdown or lease expiration cleanup.
    /// </summary>
    public void ReleaseLease()
    {
        if (Status == OutboxEntryStatus.Processing)
        {
            Status = OutboxEntryStatus.Pending;
            LeasedUntil = null;
            LeaseHolder = null;
        }
    }
}

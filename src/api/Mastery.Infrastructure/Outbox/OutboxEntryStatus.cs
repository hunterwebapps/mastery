namespace Mastery.Infrastructure.Outbox;

/// <summary>
/// Status of an outbox entry in the processing pipeline.
/// </summary>
public enum OutboxEntryStatus
{
    /// <summary>
    /// Entry is waiting to be processed.
    /// </summary>
    Pending,

    /// <summary>
    /// Entry has been leased by a worker and is being processed.
    /// </summary>
    Processing,

    /// <summary>
    /// Entry has been successfully processed.
    /// </summary>
    Processed,

    /// <summary>
    /// Entry failed to process after max retries.
    /// </summary>
    Failed
}

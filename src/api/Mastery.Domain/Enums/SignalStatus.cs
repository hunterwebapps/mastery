namespace Mastery.Domain.Enums;

/// <summary>
/// Status of a signal in the processing pipeline.
/// </summary>
public enum SignalStatus
{
    /// <summary>
    /// Signal is waiting to be processed.
    /// </summary>
    Pending,

    /// <summary>
    /// Signal has been leased by a worker and is being processed.
    /// </summary>
    Processing,

    /// <summary>
    /// Signal has been successfully processed.
    /// </summary>
    Processed,

    /// <summary>
    /// Signal was evaluated but skipped (no action needed).
    /// </summary>
    Skipped,

    /// <summary>
    /// Signal failed to process after max retries.
    /// </summary>
    Failed,

    /// <summary>
    /// Signal expired before it could be processed (TTL exceeded).
    /// </summary>
    Expired
}

namespace Mastery.Infrastructure.Outbox;

/// <summary>
/// Repository for managing outbox entries used in the transactional outbox pattern.
/// Supports lease-based batch processing for concurrent workers.
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Acquires a batch of pending outbox entries for processing.
    /// Uses row-level locking to ensure atomic lease acquisition without blocking.
    /// </summary>
    /// <param name="leaseHolder">Identifier of the worker acquiring the lease.</param>
    /// <param name="leaseUntil">When the lease expires if not completed.</param>
    /// <param name="batchSize">Maximum number of entries to acquire.</param>
    /// <param name="maxRetries">Entries with RetryCount >= this value are skipped.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of entries with acquired leases.</returns>
    Task<IReadOnlyList<OutboxEntry>> AcquireBatchAsync(
        string leaseHolder,
        DateTime leaseUntil,
        int batchSize,
        int maxRetries,
        CancellationToken ct);

    /// <summary>
    /// Releases leases on entries where the lease has expired.
    /// Used to recover from worker crashes or timeouts.
    /// </summary>
    Task ReleaseExpiredLeasesAsync(CancellationToken ct);

    /// <summary>
    /// Updates a batch of outbox entries (typically after processing).
    /// </summary>
    Task UpdateBatchAsync(IEnumerable<OutboxEntry> entries, CancellationToken ct);

    /// <summary>
    /// Archives (soft-deletes) processed entries older than the specified time.
    /// </summary>
    /// <param name="olderThan">Archive entries processed before this time.</param>
    /// <param name="batchSize">Maximum number of entries to archive per call.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Number of entries archived.</returns>
    Task<int> ArchiveProcessedEntriesAsync(DateTime olderThan, int batchSize, CancellationToken ct);
}

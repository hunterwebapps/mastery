using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;

namespace Mastery.Application.Common.Interfaces;

/// <summary>
/// Repository interface for signal queue operations.
/// </summary>
public interface ISignalQueue
{
    /// <summary>
    /// Enqueues a new signal for processing.
    /// </summary>
    Task EnqueueAsync(SignalEntry signal, CancellationToken ct = default);

    /// <summary>
    /// Enqueues multiple signals in a batch.
    /// </summary>
    Task EnqueueBatchAsync(IEnumerable<SignalEntry> signals, CancellationToken ct = default);

    /// <summary>
    /// Acquires a batch of signals for processing with a lease.
    /// Only returns signals that are pending and ready for processing.
    /// </summary>
    /// <param name="workerId">Unique identifier for the worker acquiring the lease.</param>
    /// <param name="maxPriority">Maximum priority level to acquire (inclusive). Lower value = higher priority.</param>
    /// <param name="leaseDuration">How long to hold the lease.</param>
    /// <param name="batchSize">Maximum number of signals to acquire.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of signals that were successfully leased.</returns>
    Task<IReadOnlyList<SignalEntry>> AcquireBatchAsync(
        string workerId,
        SignalPriority maxPriority,
        TimeSpan leaseDuration,
        int batchSize,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all pending signals for a user within a specific processing window.
    /// </summary>
    Task<IReadOnlyList<SignalEntry>> GetPendingForUserWindowAsync(
        string userId,
        ProcessingWindowType windowType,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all pending signals for a user.
    /// </summary>
    Task<IReadOnlyList<SignalEntry>> GetPendingForUserAsync(
        string userId,
        CancellationToken ct = default);

    /// <summary>
    /// Marks signals as processed.
    /// </summary>
    Task MarkProcessedAsync(
        IEnumerable<long> signalIds,
        AssessmentTier tier,
        CancellationToken ct = default);

    /// <summary>
    /// Marks signals as skipped.
    /// </summary>
    Task MarkSkippedAsync(
        IEnumerable<long> signalIds,
        string reason,
        CancellationToken ct = default);

    /// <summary>
    /// Gets users who have pending signals for a specific window type,
    /// grouped by timezone (for efficient batch processing).
    /// </summary>
    /// <param name="windowType">The processing window type.</param>
    /// <param name="windowStartUtc">The UTC start time of the window.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dictionary mapping timezone band to list of user IDs.</returns>
    Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetUsersForWindowByTimezoneAsync(
        ProcessingWindowType windowType,
        DateTime windowStartUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Gets user IDs with pending urgent signals.
    /// </summary>
    Task<IReadOnlyList<string>> GetUsersWithUrgentSignalsAsync(CancellationToken ct = default);

    /// <summary>
    /// Expires signals that have exceeded their TTL.
    /// </summary>
    /// <param name="now">Current UTC time.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Number of signals expired.</returns>
    Task<int> ExpireOldSignalsAsync(DateTime now, CancellationToken ct = default);

    /// <summary>
    /// Releases expired leases (cleanup for crashed workers).
    /// </summary>
    /// <param name="now">Current UTC time.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Number of leases released.</returns>
    Task<int> ReleaseExpiredLeasesAsync(DateTime now, CancellationToken ct = default);

    /// <summary>
    /// Gets a signal by ID.
    /// </summary>
    Task<SignalEntry?> GetByIdAsync(long signalId, CancellationToken ct = default);

    /// <summary>
    /// Gets signal count by status for monitoring.
    /// </summary>
    Task<IReadOnlyDictionary<SignalStatus, int>> GetStatusCountsAsync(CancellationToken ct = default);
}

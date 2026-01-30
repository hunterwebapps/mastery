using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;

namespace Mastery.Application.Common.Interfaces;

/// <summary>
/// Repository interface for signal processing history operations.
/// </summary>
public interface ISignalProcessingHistoryRepository
{
    /// <summary>
    /// Adds a new processing history record.
    /// </summary>
    Task AddAsync(SignalProcessingHistory history, CancellationToken ct = default);

    /// <summary>
    /// Gets a processing history record by ID.
    /// </summary>
    Task<SignalProcessingHistory?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets the last completed processing history for a user.
    /// </summary>
    Task<SignalProcessingHistory?> GetLastForUserAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a batch has already been processed (for idempotency).
    /// </summary>
    Task<bool> ExistsByBatchIdAsync(Guid batchId, CancellationToken ct = default);

    /// <summary>
    /// Gets recent processing history records.
    /// </summary>
    Task<IReadOnlyList<SignalProcessingHistory>> GetRecentAsync(int count, CancellationToken ct = default);

    /// <summary>
    /// Gets processing history for a user within a date range.
    /// </summary>
    Task<IReadOnlyList<SignalProcessingHistory>> GetForUserInRangeAsync(
        string userId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Gets aggregated statistics for a window type.
    /// </summary>
    Task<ProcessingStatistics> GetStatisticsAsync(
        ProcessingWindowType? windowType,
        DateTime sinceUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing processing history record.
    /// </summary>
    Task UpdateAsync(SignalProcessingHistory history, CancellationToken ct = default);
}

/// <summary>
/// Aggregated statistics for signal processing.
/// </summary>
public sealed record ProcessingStatistics(
    int TotalCycles,
    int TotalSignalsProcessed,
    int TotalSignalsSkipped,
    int TotalRecommendationsGenerated,
    int CyclesWithErrors,
    double AverageDurationMs,
    IReadOnlyDictionary<AssessmentTier, int> TierDistribution);

using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;

namespace Mastery.Application.Common.Interfaces;

/// <summary>
/// Repository interface for signal queue operations.
/// </summary>
public interface ISignalEntryRepository
{
    /// <summary>
    /// Enqueues multiple signals in a batch.
    /// </summary>
    Task AddRangeAsync(IEnumerable<SignalEntry> signals, CancellationToken ct = default);

    /// <summary>
    /// Checks if a window signal already exists for the given user, event type, and date.
    /// Used for deduplication when scheduling window signals.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="eventType">The event type (e.g., "MorningWindowStart").</param>
    /// <param name="windowDate">The date of the window (UTC).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if a signal already exists for this user/event/date combination.</returns>
    Task<bool> ExistsForWindowAsync(
        string userId,
        string eventType,
        DateOnly windowDate,
        CancellationToken ct = default);
}

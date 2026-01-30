using Mastery.Domain.Entities.Signal;

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
}

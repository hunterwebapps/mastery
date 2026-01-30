using Mastery.Domain.Common;

namespace Mastery.Application.Common.Interfaces;

/// <summary>
/// Detects derived (urgent) signals based on user state patterns.
/// Called after certain domain events to check for escalation conditions.
/// </summary>
public interface IDerivedSignalDetector
{
    /// <summary>
    /// Checks for derived signals triggered by a domain event.
    /// Returns any P0 (urgent) events that should be raised based on the current state.
    /// </summary>
    /// <param name="triggeringEvent">The domain event that may trigger derived signals.</param>
    /// <param name="userId">The user ID associated with the event.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of derived domain events (P0 signals) to be raised.</returns>
    Task<IReadOnlyList<IDomainEvent>> DetectDerivedSignalsAsync(
        IDomainEvent triggeringEvent,
        string userId,
        CancellationToken ct = default);
}

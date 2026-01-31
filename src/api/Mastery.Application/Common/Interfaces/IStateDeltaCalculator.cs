using Mastery.Application.Common.Models;
using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Entities.Signal;

namespace Mastery.Application.Common.Interfaces;

/// <summary>
/// Calculates the state delta (changes) since the last recommendation assessment.
/// Used by Tier 1 to determine if significant changes warrant full LLM evaluation.
/// </summary>
public interface IStateDeltaCalculator
{
    /// <summary>
    /// Calculates the state delta for a user since their last assessment.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="currentState">The current user state snapshot.</param>
    /// <param name="signals">The signals being processed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A summary of state changes and a delta score (0-1).</returns>
    Task<StateDeltaSummary> CalculateAsync(
        string userId,
        UserStateSnapshot currentState,
        IReadOnlyList<SignalEntry> signals,
        CancellationToken ct = default);

    /// <summary>
    /// Records the current state as the baseline for future delta calculations.
    /// Called after a successful Tier 2 assessment.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="state">The state snapshot to record.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RecordBaselineAsync(
        string userId,
        UserStateSnapshot state,
        CancellationToken ct = default);
}

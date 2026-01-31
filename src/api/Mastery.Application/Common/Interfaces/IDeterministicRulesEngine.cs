using Mastery.Application.Common.Models;
using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Entities.Signal;

namespace Mastery.Application.Common.Interfaces;

/// <summary>
/// Tier 0 deterministic rules engine that evaluates user state against
/// predefined rules to detect issues and generate recommendations without LLM.
/// </summary>
public interface IDeterministicRulesEngine
{
    /// <summary>
    /// Evaluates all registered rules against the user's current state.
    /// </summary>
    /// <param name="state">The user's current state snapshot.</param>
    /// <param name="signals">Pending signals that triggered this evaluation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Aggregated evaluation result with triggered rules and direct recommendations.</returns>
    Task<RuleEvaluationResult> EvaluateAsync(
        UserStateSnapshot state,
        IReadOnlyList<SignalEntry> signals,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the list of all registered rule IDs.
    /// </summary>
    IReadOnlyList<string> RegisteredRuleIds { get; }
}

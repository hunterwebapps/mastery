using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Entities.Signal;

namespace Mastery.Domain.Diagnostics;

/// <summary>
/// A single deterministic rule that can be evaluated against user state.
/// </summary>
public interface IDeterministicRule
{
    /// <summary>
    /// Unique identifier for this rule.
    /// </summary>
    string RuleId { get; }

    /// <summary>
    /// Human-readable name for this rule.
    /// </summary>
    string RuleName { get; }

    /// <summary>
    /// Description of what this rule detects.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Whether this rule is enabled by default.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Evaluates the rule against the user's state.
    /// </summary>
    /// <param name="state">The user's current state snapshot.</param>
    /// <param name="signals">Pending signals that triggered this evaluation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The rule evaluation result.</returns>
    Task<RuleResult> EvaluateAsync(
        UserStateSnapshot state,
        IReadOnlyList<SignalEntry> signals,
        CancellationToken ct = default);
}

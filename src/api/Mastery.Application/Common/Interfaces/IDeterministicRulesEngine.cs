using Mastery.Application.Common.Models;
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

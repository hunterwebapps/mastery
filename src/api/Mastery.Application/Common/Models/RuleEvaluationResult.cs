using Mastery.Domain.Diagnostics;

namespace Mastery.Application.Common.Models;

/// <summary>
/// Aggregated result of running all deterministic rules.
/// </summary>
public sealed record RuleEvaluationResult(
    IReadOnlyList<RuleResult> AllResults,
    IReadOnlyList<RuleResult> TriggeredRules,
    IReadOnlyList<DirectRecommendationCandidate> DirectRecommendations,
    bool ShouldEscalateToTier1,
    string? EscalationReason)
{
    /// <summary>
    /// Gets the highest severity among triggered rules.
    /// </summary>
    public RuleSeverity? MaxSeverity => TriggeredRules.Count > 0
        ? TriggeredRules.Max(r => r.Severity)
        : null;

    /// <summary>
    /// Gets the count of triggered rules by severity.
    /// </summary>
    public IReadOnlyDictionary<RuleSeverity, int> SeverityCounts => TriggeredRules
        .GroupBy(r => r.Severity)
        .ToDictionary(g => g.Key, g => g.Count());
}

namespace Mastery.Domain.Diagnostics;

/// <summary>
/// Result of evaluating a single deterministic rule.
/// </summary>
public sealed record RuleResult(
    string RuleId,
    string RuleName,
    bool Triggered,
    RuleSeverity Severity,
    IReadOnlyDictionary<string, object> Evidence,
    DirectRecommendationCandidate? DirectRecommendation = null,
    bool RequiresEscalation = false);

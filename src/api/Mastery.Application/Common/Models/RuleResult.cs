using Mastery.Domain.Enums;
using Mastery.Domain.ValueObjects;

namespace Mastery.Application.Common.Models;

/// <summary>
/// Severity level for a triggered rule.
/// </summary>
public enum RuleSeverity
{
    Low,
    Medium,
    High,
    Critical
}

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

/// <summary>
/// A recommendation that can be generated directly from a rule without LLM involvement.
/// </summary>
public sealed record DirectRecommendationCandidate(
    RecommendationType Type,
    RecommendationContext Context,
    RecommendationTargetKind TargetKind,
    Guid? TargetEntityId,
    string? TargetEntityTitle,
    RecommendationActionKind ActionKind,
    string Title,
    string Rationale,
    decimal Score,
    string? ActionPayload = null,
    string? ActionSummary = null);

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

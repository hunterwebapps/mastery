using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Entities.Signal;

namespace Mastery.Domain.Diagnostics;

/// <summary>
/// Base class for deterministic rules providing common functionality.
/// </summary>
public abstract class DeterministicRuleBase : IDeterministicRule
{
    public abstract string RuleId { get; }
    public abstract string RuleName { get; }
    public abstract string Description { get; }
    public virtual bool IsEnabled => true;

    public abstract Task<RuleResult> EvaluateAsync(
        UserStateSnapshot state,
        IReadOnlyList<SignalEntry> signals,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a non-triggered rule result.
    /// </summary>
    protected RuleResult NotTriggered() => new(
        RuleId,
        RuleName,
        Triggered: false,
        Severity: RuleSeverity.Low,
        Evidence: new Dictionary<string, object>());

    /// <summary>
    /// Creates a triggered rule result.
    /// </summary>
    protected RuleResult Triggered(
        RuleSeverity severity,
        Dictionary<string, object> evidence,
        DirectRecommendationCandidate? directRecommendation = null,
        bool requiresEscalation = false) => new(
        RuleId,
        RuleName,
        Triggered: true,
        Severity: severity,
        Evidence: evidence,
        DirectRecommendation: directRecommendation,
        RequiresEscalation: requiresEscalation);
}

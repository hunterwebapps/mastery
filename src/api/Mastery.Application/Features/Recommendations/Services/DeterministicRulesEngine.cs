using Mastery.Application.Common.Interfaces;
using Mastery.Application.Common.Models;
using Mastery.Domain.Diagnostics;
using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Mastery.Application.Features.Recommendations.Services;

/// <summary>
/// Tier 0 rules engine that evaluates all registered deterministic rules
/// and aggregates results for escalation decisions.
/// </summary>
public sealed class DeterministicRulesEngine(
    IEnumerable<IDeterministicRule> _rules,
    ILogger<DeterministicRulesEngine> _logger)
    : IDeterministicRulesEngine
{
    public IReadOnlyList<string> RegisteredRuleIds => _rules
        .Where(r => r.IsEnabled)
        .Select(r => r.RuleId)
        .ToList();

    public async Task<RuleEvaluationResult> EvaluateAsync(
        UserStateSnapshot state,
        IReadOnlyList<SignalEntry> signals,
        CancellationToken ct = default)
    {
        var allResults = new List<RuleResult>();
        var enabledRules = _rules.Where(r => r.IsEnabled).ToList();

        _logger.LogDebug(
            "Evaluating {RuleCount} deterministic rules for user {UserId}",
            enabledRules.Count,
            state.UserId);

        // Evaluate all rules in parallel for efficiency
        var evaluationTasks = enabledRules.Select(async rule =>
        {
            try
            {
                return await rule.EvaluateAsync(state, signals, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Rule {RuleId} failed for user {UserId}: {Error}",
                    rule.RuleId,
                    state.UserId,
                    ex.Message);

                // Return a non-triggered result on error
                return new RuleResult(
                    rule.RuleId,
                    rule.RuleName,
                    Triggered: false,
                    Severity: RuleSeverity.Low,
                    Evidence: new Dictionary<string, object> { ["Error"] = ex.Message });
            }
        });

        var results = await Task.WhenAll(evaluationTasks);
        allResults.AddRange(results);

        var triggeredRules = allResults.Where(r => r.Triggered).ToList();

        _logger.LogInformation(
            "Tier 0 evaluation complete for user {UserId}: {TriggeredCount}/{TotalCount} rules triggered",
            state.UserId,
            triggeredRules.Count,
            allResults.Count);

        // Collect direct recommendations from triggered rules
        var directRecommendations = triggeredRules
            .Where(r => r.DirectRecommendation != null)
            .Select(r => r.DirectRecommendation!)
            .OrderByDescending(r => r.Score)
            .ToList();

        // Determine if escalation to Tier 1 is needed
        var (shouldEscalate, escalationReason) = DetermineEscalation(triggeredRules, directRecommendations);

        if (triggeredRules.Count > 0)
        {
            _logger.LogDebug(
                "Triggered rules for user {UserId}: {RuleIds}. Escalate: {ShouldEscalate}",
                state.UserId,
                string.Join(", ", triggeredRules.Select(r => r.RuleId)),
                shouldEscalate);
        }

        return new RuleEvaluationResult(
            AllResults: allResults,
            TriggeredRules: triggeredRules,
            DirectRecommendations: directRecommendations,
            ShouldEscalateToTier1: shouldEscalate,
            EscalationReason: escalationReason);
    }

    private static (bool ShouldEscalate, string? Reason) DetermineEscalation(
        IReadOnlyList<RuleResult> triggeredRules,
        IReadOnlyList<DirectRecommendationCandidate> directRecommendations)
    {
        // No rules triggered = no need for any assessment
        if (triggeredRules.Count == 0)
            return (false, null);

        // Any rule explicitly requesting escalation
        var explicitEscalation = triggeredRules.FirstOrDefault(r => r.RequiresEscalation);
        if (explicitEscalation != null)
            return (true, $"Rule {explicitEscalation.RuleId} requires deeper assessment");

        // Multiple high/critical severity rules = complex situation needing LLM judgment
        var highSeverityCount = triggeredRules.Count(r =>
            r.Severity is RuleSeverity.High or RuleSeverity.Critical);

        if (highSeverityCount >= 2)
            return (true, $"Multiple high-severity issues detected ({highSeverityCount})");

        // Conflicting recommendations (e.g., defer vs execute today)
        if (HasConflictingRecommendations(directRecommendations))
            return (true, "Conflicting recommendations require prioritization");

        // Too many triggered rules suggests complex state
        if (triggeredRules.Count >= 4)
            return (true, $"Many issues detected ({triggeredRules.Count}) - needs holistic assessment");

        // Default: no escalation, direct recommendations are sufficient
        return (false, null);
    }

    private static bool HasConflictingRecommendations(IReadOnlyList<DirectRecommendationCandidate> recommendations)
    {
        if (recommendations.Count < 2)
            return false;

        // Check for conflicting action kinds on the same target
        var groupedByTarget = recommendations
            .Where(r => r.TargetEntityId.HasValue)
            .GroupBy(r => r.TargetEntityId);

        foreach (var group in groupedByTarget)
        {
            var actionKinds = group.Select(r => r.ActionKind).Distinct().ToList();
            if (actionKinds.Count > 1)
            {
                // Multiple different actions suggested for same entity
                return true;
            }
        }

        // Check for execute vs defer conflicts
        var hasExecuteToday = recommendations.Any(r =>
            r.ActionKind == RecommendationActionKind.ExecuteToday);
        var hasDefer = recommendations.Any(r =>
            r.ActionKind == RecommendationActionKind.Defer);

        return hasExecuteToday && hasDefer;
    }
}

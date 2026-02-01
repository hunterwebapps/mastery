using Mastery.Application.Common.Interfaces;
using Mastery.Application.Common.Models;
using Mastery.Application.Features.Learning.Services;
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
    ILearningEngineService _learningEngine,
    IUserPlaybookRepository _playbookRepository,
    IUserContextProvider _userContextProvider,
    ILogger<DeterministicRulesEngine> _logger)
    : IDeterministicRulesEngine
{
    // Epsilon-greedy exploration parameters
    private const decimal InitialEpsilon = 0.20m;      // Start with 20% exploration
    private const decimal MinEpsilon = 0.05m;          // Floor at 5% exploration
    private const decimal DecayFactor = 0.95m;         // Decay rate per 100 outcomes
    private static readonly Random _random = new();
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
        var rawRecommendations = triggeredRules
            .Where(r => r.DirectRecommendation != null)
            .Select(r => r.DirectRecommendation!)
            .ToList();

        // Apply learned weights to adjust scores based on user's historical outcomes
        var directRecommendations = await ApplyLearnedWeightsAsync(
            state.UserId,
            rawRecommendations,
            ct);

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
            allResults,
            triggeredRules,
            directRecommendations,
            shouldEscalate,
            escalationReason);
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

    /// <summary>
    /// Applies learned weights from user's playbook to adjust recommendation scores.
    /// Weight acts as a multiplier (0.1 to 0.95) based on historical success.
    /// Includes epsilon-greedy exploration to avoid local optima.
    /// </summary>
    private async Task<IReadOnlyList<DirectRecommendationCandidate>> ApplyLearnedWeightsAsync(
        string userId,
        List<DirectRecommendationCandidate> recommendations,
        CancellationToken ct)
    {
        if (recommendations.Count == 0)
            return recommendations;

        // Build current context for weight lookup
        var context = await _userContextProvider.GetCurrentContextAsync(userId, ct);

        // Get weights for all recommendation types in batch
        var types = recommendations.Select(r => r.Type).Distinct();
        var weights = await _learningEngine.GetWeightsForTypesAsync(userId, types, context, ct);

        // Calculate exploration epsilon based on playbook maturity
        var playbook = await _playbookRepository.GetByUserIdAsync(userId, ct);
        var epsilon = CalculateEpsilon(playbook?.TotalOutcomes ?? 0);

        // Apply weights: adjustedScore = baseScore * weightFactor
        // Weight of 0.5 = neutral (no change)
        // Weight > 0.5 = boost score (user historically accepts/completes this type)
        // Weight < 0.5 = reduce score (user historically dismisses this type)
        var adjusted = recommendations.Select(r =>
        {
            var weight = weights.TryGetValue(r.Type, out var w) ? w : 0.5m;

            // Convert weight (0.1-0.95) to multiplier (0.7-1.3)
            // 0.5 -> 1.0 (neutral)
            // 0.1 -> 0.7 (30% reduction)
            // 0.95 -> 1.3 (30% boost)
            var multiplier = 0.7m + (weight - 0.1m) * (0.6m / 0.85m);
            var adjustedScore = Math.Clamp(r.Score * multiplier, 0.1m, 1.0m);

            _logger.LogDebug(
                "Applied learned weight for {Type}: base={BaseScore:F3}, weight={Weight:F3}, adjusted={AdjustedScore:F3}",
                r.Type, r.Score, weight, adjustedScore);

            return r with { Score = adjustedScore };
        })
        .ToList();

        // Apply epsilon-greedy exploration
        adjusted = ApplyExploration(adjusted, epsilon);

        // Final ordering by score
        return adjusted.OrderByDescending(r => r.Score).ToList();
    }

    /// <summary>
    /// Calculates exploration epsilon based on total outcomes recorded.
    /// Starts at 20%, decays to 5% floor as user provides more feedback.
    /// </summary>
    private static decimal CalculateEpsilon(int totalOutcomes)
    {
        // Decay epsilon by 5% per 100 outcomes
        var decayExponent = (decimal)totalOutcomes / 100.0m;
        var epsilon = InitialEpsilon * (decimal)Math.Pow((double)DecayFactor, (double)decayExponent);
        return Math.Max(MinEpsilon, epsilon);
    }

    /// <summary>
    /// Applies epsilon-greedy exploration by occasionally shuffling lower-ranked candidates higher.
    /// This prevents the system from getting stuck always recommending the same types.
    /// </summary>
    private List<DirectRecommendationCandidate> ApplyExploration(
        List<DirectRecommendationCandidate> candidates,
        decimal epsilon)
    {
        if (candidates.Count < 2)
            return candidates;

        // Roll the dice: explore with probability epsilon
        var roll = (decimal)_random.NextDouble();
        if (roll >= epsilon)
        {
            _logger.LogDebug(
                "Exploitation mode (roll={Roll:F3} >= epsilon={Epsilon:F3})",
                roll, epsilon);
            return candidates;
        }

        // Exploration: randomly boost one non-top candidate
        var nonTopCandidates = candidates
            .Skip(1) // Exclude the top candidate
            .ToList();

        if (nonTopCandidates.Count == 0)
            return candidates;

        // Pick a random candidate to explore
        var exploreIndex = _random.Next(nonTopCandidates.Count);
        var exploreCandidate = nonTopCandidates[exploreIndex];

        // Boost its score to be competitive with the top
        var topScore = candidates[0].Score;
        var boostedScore = Math.Min(1.0m, exploreCandidate.Score + (topScore - exploreCandidate.Score) * 0.8m);

        var result = candidates.Select(c =>
            c == exploreCandidate ? c with { Score = boostedScore } : c
        ).ToList();

        _logger.LogInformation(
            "Exploration: boosted {Type} from {OldScore:F3} to {NewScore:F3} (epsilon={Epsilon:F3})",
            exploreCandidate.Type,
            exploreCandidate.Score,
            boostedScore,
            epsilon);

        return result;
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

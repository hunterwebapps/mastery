using Mastery.Application.Common.Models;
using Mastery.Domain.Entities.Goal;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;

namespace Mastery.Infrastructure.Services.Rules;

/// <summary>
/// Detects active goals that are missing lead or lag metrics on their scoreboard.
/// A complete scoreboard has at least one lag metric (outcome) and one lead metric (predictor).
/// </summary>
public sealed class GoalScoreboardIncompleteRule : DeterministicRuleBase
{
    public override string RuleId => "GOAL_SCOREBOARD_INCOMPLETE";
    public override string RuleName => "Incomplete Goal Scoreboard";
    public override string Description => "Detects active goals missing lead or lag metrics needed for effective tracking.";

    public override Task<RuleResult> EvaluateAsync(
        UserStateSnapshot state,
        IReadOnlyList<SignalEntry> signals,
        CancellationToken ct = default)
    {
        // Early exit if no relevant signals in the batch
        var relevantSignals = signals.Where(s =>
            s.EventType == nameof(GoalCreatedEvent) ||
            s.EventType == nameof(GoalUpdatedEvent) ||
            s.EventType == nameof(GoalScoreboardUpdatedEvent) ||
            s.EventType == "MorningWindowStart").ToList();

        if (relevantSignals.Count == 0)
            return Task.FromResult(NotTriggered());

        // Get IDs of goals created in this signal batch (grace period - user hasn't had time to add metrics)
        var newlyCreatedGoalIds = signals
            .Where(s => s.EventType == nameof(GoalCreatedEvent) && s.TargetEntityId.HasValue)
            .Select(s => s.TargetEntityId!.Value)
            .ToHashSet();

        var incompleteGoals = new List<(GoalSnapshot Goal, bool HasLag, bool HasLead)>();

        foreach (var goal in state.Goals.Where(g =>
            g.Status == GoalStatus.Active &&
            !newlyCreatedGoalIds.Contains(g.Id)))
        {
            var hasLagMetric = goal.Metrics.Any(m => m.Kind == MetricKind.Lag);
            var hasLeadMetric = goal.Metrics.Any(m => m.Kind == MetricKind.Lead);

            // Goal needs at least one of each type for a "complete" scoreboard
            if (!hasLagMetric || !hasLeadMetric)
            {
                incompleteGoals.Add((goal, hasLagMetric, hasLeadMetric));
            }
        }

        if (incompleteGoals.Count == 0)
            return Task.FromResult(NotTriggered());

        // Prioritize goals with higher priority or closer deadlines
        var mostUrgent = incompleteGoals
            .OrderBy(g => g.Goal.Priority)
            .ThenBy(g => g.Goal.Deadline ?? DateOnly.MaxValue)
            .First();

        var missingTypes = new List<string>();
        if (!mostUrgent.HasLag) missingTypes.Add("lag (outcome)");
        if (!mostUrgent.HasLead) missingTypes.Add("lead (predictor)");

        // Count P1 incomplete goals for escalation decision
        var p1IncompleteCount = incompleteGoals.Count(g => g.Goal.Priority == 1);

        // Enhanced severity: consider both priority and incomplete goal count
        var severity = (mostUrgent.Goal.Priority, incompleteGoals.Count) switch
        {
            (1, >= 3) => RuleSeverity.Critical,  // P1 + many incomplete
            (1, _) => RuleSeverity.High,
            (2, >= 3) => RuleSeverity.High,
            (2, _) => RuleSeverity.Medium,
            (_, >= 3) => RuleSeverity.Medium,
            _ => RuleSeverity.Low
        };

        // Dynamic score calculation: base + priority bonus + volume bonus
        var baseScore = 0.50m;
        var priorityBonus = mostUrgent.Goal.Priority switch
        {
            1 => 0.20m,  // P1 goals: +20%
            2 => 0.10m,  // P2 goals: +10%
            _ => 0.0m
        };
        var volumeBonus = Math.Min(incompleteGoals.Count * 0.05m, 0.15m); // +5% per goal, max +15%
        var score = Math.Min(baseScore + priorityBonus + volumeBonus, 0.85m);

        // Escalate to Tier 2 when multiple P1 goals need attention - LLM can help prioritize setup order
        var requiresEscalation = p1IncompleteCount >= 2;

        var evidence = new Dictionary<string, object>
        {
            ["IncompleteGoalCount"] = incompleteGoals.Count,
            ["P1IncompleteCount"] = p1IncompleteCount,
            ["MostUrgentGoalId"] = mostUrgent.Goal.Id,
            ["MostUrgentGoalTitle"] = mostUrgent.Goal.Title,
            ["MostUrgentPriority"] = mostUrgent.Goal.Priority,
            ["HasLagMetric"] = mostUrgent.HasLag,
            ["HasLeadMetric"] = mostUrgent.HasLead,
            ["MissingMetricTypes"] = missingTypes,
            ["TriggeringSignalType"] = relevantSignals.FirstOrDefault()?.EventType ?? "Unknown",
            ["LeadMetricCount"] = mostUrgent.Goal.Metrics.Count(m => m.Kind == MetricKind.Lead),
            ["LagMetricCount"] = mostUrgent.Goal.Metrics.Count(m => m.Kind == MetricKind.Lag),
            ["AllIncompleteGoals"] = incompleteGoals.Select(g => new
            {
                g.Goal.Id,
                g.Goal.Title,
                g.Goal.Priority,
                g.HasLag,
                g.HasLead
            }).ToList()
        };

        var directRecommendation = new DirectRecommendationCandidate(
            Type: RecommendationType.GoalScoreboardSuggestion,
            Context: RecommendationContext.ProactiveCheck,
            TargetKind: RecommendationTargetKind.Goal,
            TargetEntityId: mostUrgent.Goal.Id,
            TargetEntityTitle: mostUrgent.Goal.Title,
            ActionKind: RecommendationActionKind.Update,
            Title: $"Complete the scoreboard for \"{mostUrgent.Goal.Title}\"",
            Rationale: $"This goal is missing {string.Join(" and ", missingTypes)} metrics. Lead metrics predict success; lag metrics measure outcomes. Both are needed to track progress effectively and catch drift early.",
            Score: score,
            ActionSummary: $"Add {string.Join(", ", missingTypes)} metric(s)");

        return Task.FromResult(Triggered(severity, evidence, directRecommendation, requiresEscalation));
    }
}

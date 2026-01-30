using Mastery.Application.Common.Models;
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
        var incompleteGoals = new List<(GoalSnapshot Goal, bool HasLag, bool HasLead)>();

        foreach (var goal in state.Goals.Where(g => g.Status == GoalStatus.Active))
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

        var severity = mostUrgent.Goal.Priority switch
        {
            1 => RuleSeverity.High,
            2 => RuleSeverity.Medium,
            _ => RuleSeverity.Low
        };

        var evidence = new Dictionary<string, object>
        {
            ["IncompleteGoalCount"] = incompleteGoals.Count,
            ["MostUrgentGoalId"] = mostUrgent.Goal.Id,
            ["MostUrgentGoalTitle"] = mostUrgent.Goal.Title,
            ["MostUrgentPriority"] = mostUrgent.Goal.Priority,
            ["HasLagMetric"] = mostUrgent.HasLag,
            ["HasLeadMetric"] = mostUrgent.HasLead,
            ["MissingMetricTypes"] = missingTypes,
            ["AllIncompleteGoals"] = incompleteGoals.Select(g => new
            {
                g.Goal.Id,
                g.Goal.Title,
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
            Score: 0.65m,
            ActionSummary: $"Add {string.Join(", ", missingTypes)} metric(s)");

        return Task.FromResult(Triggered(severity, evidence, directRecommendation));
    }
}

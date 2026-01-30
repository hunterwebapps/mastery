using Mastery.Application.Common.Models;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;
using GoalCreatedEvent = Mastery.Domain.Entities.Goal.GoalCreatedEvent;
using GoalScoreboardUpdatedEvent = Mastery.Domain.Entities.Goal.GoalScoreboardUpdatedEvent;
using GoalUpdatedEvent = Mastery.Domain.Entities.Goal.GoalUpdatedEvent;
using MetricObservationRecordedEvent = Mastery.Domain.Entities.Metrics.MetricObservationRecordedEvent;

namespace Mastery.Infrastructure.Services.Rules;

/// <summary>
/// Detects goals where current progress rate is insufficient to meet the deadline.
/// Triggers when: Goal has deadline approaching and progress trajectory won't reach target.
/// </summary>
public sealed class GoalProgressAtRiskRule : DeterministicRuleBase
{
    private const int MinDaysToTrigger = 7;          // Don't trigger if deadline very far out
    private const int CriticalDaysRemaining = 7;
    private const int HighDaysRemaining = 14;
    private const decimal CriticalRateRatio = 0.25m; // Progress rate is <25% of required
    private const decimal HighRateRatio = 0.50m;     // Progress rate is <50% of required
    private const decimal MediumRateRatio = 0.75m;   // Progress rate is <75% of required

    public override string RuleId => "GOAL_PROGRESS_AT_RISK";
    public override string RuleName => "Goal Progress At Risk";
    public override string Description => "Detects goals where current progress rate is insufficient to meet the deadline.";

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
            s.EventType == nameof(MetricObservationRecordedEvent) ||
            s.EventType == "MorningWindowStart").ToList();

        if (relevantSignals.Count == 0)
            return Task.FromResult(NotTriggered());

        var atRiskGoals = new List<GoalProgressAnalysis>();

        foreach (var goal in state.Goals.Where(g =>
            g.Status == GoalStatus.Active &&
            g.Deadline.HasValue &&
            g.Metrics.Count > 0))
        {
            var deadline = goal.Deadline!.Value;
            var daysRemaining = deadline.DayNumber - state.Today.DayNumber;

            // Skip if deadline is too far in the future
            if (daysRemaining > 90)
                continue;

            // Skip if already past deadline (DeadlineProximityRule handles overdue)
            if (daysRemaining <= 0)
                continue;

            var analysis = AnalyzeGoalProgress(goal, state.Today, daysRemaining);

            if (analysis.IsAtRisk)
            {
                atRiskGoals.Add(analysis);
            }
        }

        if (atRiskGoals.Count == 0)
            return Task.FromResult(NotTriggered());

        // Prioritize by: priority first, then how at-risk (lowest rate ratio worst)
        var mostAtRisk = atRiskGoals
            .OrderBy(g => g.Goal.Priority)
            .ThenBy(g => g.RateRatio)
            .First();

        var severity = ComputeSeverity(
            mostAtRisk.RateRatio,
            mostAtRisk.DaysRemaining,
            mostAtRisk.Goal.Priority);

        var evidence = new Dictionary<string, object>
        {
            ["AtRiskGoalCount"] = atRiskGoals.Count,
            ["MostAtRiskGoalId"] = mostAtRisk.Goal.Id,
            ["MostAtRiskGoalTitle"] = mostAtRisk.Goal.Title,
            ["MostAtRiskPriority"] = mostAtRisk.Goal.Priority,
            ["DaysRemaining"] = mostAtRisk.DaysRemaining,
            ["CurrentProgress"] = Math.Round(mostAtRisk.CurrentProgress * 100, 1),
            ["RequiredProgressRate"] = Math.Round(mostAtRisk.RequiredDailyRate * 100, 2),
            ["ActualProgressRate"] = Math.Round(mostAtRisk.ActualDailyRate * 100, 2),
            ["RateRatio"] = Math.Round(mostAtRisk.RateRatio * 100, 1),
            ["ProjectedCompletion"] = Math.Round(mostAtRisk.ProjectedCompletion * 100, 1),
            ["AllAtRiskGoals"] = atRiskGoals.Select(g => new
            {
                g.Goal.Id,
                g.Goal.Title,
                g.Goal.Priority,
                g.DaysRemaining,
                CurrentProgress = Math.Round(g.CurrentProgress * 100, 1),
                RateRatio = Math.Round(g.RateRatio * 100, 1)
            }).ToList()
        };

        // Determine action based on severity
        var actionKind = severity == RuleSeverity.Critical
            ? RecommendationActionKind.ExecuteToday
            : RecommendationActionKind.ReflectPrompt;

        var progressPercent = Math.Round(mostAtRisk.CurrentProgress * 100);
        var projectedPercent = Math.Round(mostAtRisk.ProjectedCompletion * 100);

        var title = severity == RuleSeverity.Critical
            ? $"\"{mostAtRisk.Goal.Title}\" at risk - {mostAtRisk.DaysRemaining} days left, {progressPercent}% done"
            : $"\"{mostAtRisk.Goal.Title}\" progress lagging ({progressPercent}% with {mostAtRisk.DaysRemaining} days to go)";

        var rationale = BuildRationale(mostAtRisk, severity);

        var score = ComputeScore(mostAtRisk.RateRatio, mostAtRisk.DaysRemaining, mostAtRisk.Goal.Priority);

        var directRecommendation = new DirectRecommendationCandidate(
            Type: RecommendationType.NextBestAction,
            Context: RecommendationContext.DriftAlert,
            TargetKind: RecommendationTargetKind.Goal,
            TargetEntityId: mostAtRisk.Goal.Id,
            TargetEntityTitle: mostAtRisk.Goal.Title,
            ActionKind: actionKind,
            Title: title,
            Rationale: rationale,
            Score: score,
            ActionSummary: severity == RuleSeverity.Critical
                ? "Prioritize lead activities today"
                : "Review goal strategy and lead metrics");

        return Task.FromResult(Triggered(severity, evidence, directRecommendation));
    }

    private static GoalProgressAnalysis AnalyzeGoalProgress(
        GoalSnapshot goal,
        DateOnly today,
        int daysRemaining)
    {
        // Calculate weighted progress across all metrics
        var currentProgress = 0m;
        var totalWeight = 0m;
        var hasAnyProgress = false;

        foreach (var metric in goal.Metrics)
        {
            if (metric.TargetValue == 0)
                continue;

            var metricProgress = metric.CurrentValue.HasValue
                ? Math.Min(metric.CurrentValue.Value / metric.TargetValue, 1m)
                : 0m;

            currentProgress += metricProgress * metric.Weight;
            totalWeight += metric.Weight;

            if (metric.CurrentValue.HasValue && metric.CurrentValue.Value > 0)
                hasAnyProgress = true;
        }

        currentProgress = totalWeight > 0 ? currentProgress / totalWeight : 0m;

        // Calculate required daily rate to reach 100%
        var remainingProgress = 1m - currentProgress;
        var requiredDailyRate = daysRemaining > 0 ? remainingProgress / daysRemaining : 1m;

        // Estimate actual daily rate from elapsed progress
        // Use deadline to calculate total goal duration, then derive rate from elapsed time
        var totalDuration = goal.Deadline!.Value.DayNumber - EstimateGoalStartDate(goal, today).DayNumber;
        var elapsedDays = Math.Max(1, totalDuration - daysRemaining);
        var actualDailyRate = currentProgress / elapsedDays;

        // Rate ratio: how much of the required rate are we achieving?
        var rateRatio = requiredDailyRate > 0 ? actualDailyRate / requiredDailyRate : 1m;

        // Project where we'll be at deadline if we continue at current rate
        var projectedCompletion = Math.Min(currentProgress + (actualDailyRate * daysRemaining), 1m);

        // Consider at risk if:
        // 1. Rate ratio is below threshold (not progressing fast enough)
        // 2. Projected completion is significantly below 100%
        // 3. We're past minimum trigger window
        var isAtRisk = rateRatio < MediumRateRatio &&
                       projectedCompletion < 0.9m &&
                       daysRemaining <= 90 &&
                       hasAnyProgress; // Don't flag if no progress tracking started yet

        return new GoalProgressAnalysis(
            Goal: goal,
            DaysRemaining: daysRemaining,
            CurrentProgress: currentProgress,
            RequiredDailyRate: requiredDailyRate,
            ActualDailyRate: actualDailyRate,
            RateRatio: rateRatio,
            ProjectedCompletion: projectedCompletion,
            IsAtRisk: isAtRisk);
    }

    private static DateOnly EstimateGoalStartDate(GoalSnapshot goal, DateOnly today)
    {
        // Estimate goal started 30 days before deadline if no other info available
        // This is a simplification - in production, you'd track actual goal creation date
        if (!goal.Deadline.HasValue)
            return today.AddDays(-30);

        var totalDays = Math.Max(30, goal.Deadline.Value.DayNumber - today.DayNumber + 30);
        return goal.Deadline.Value.AddDays(-totalDays);
    }

    private static RuleSeverity ComputeSeverity(decimal rateRatio, int daysRemaining, int priority)
    {
        // Critical: very behind (<25% required rate) AND deadline soon (<7 days) OR P1 goal severely behind
        if ((rateRatio < CriticalRateRatio && daysRemaining <= CriticalDaysRemaining) ||
            (priority == 1 && rateRatio < CriticalRateRatio))
        {
            return RuleSeverity.Critical;
        }

        // High: moderately behind (<50% required rate) OR deadline approaching (<14 days with <50% rate)
        if (rateRatio < HighRateRatio ||
            (daysRemaining <= HighDaysRemaining && rateRatio < MediumRateRatio))
        {
            return RuleSeverity.High;
        }

        // Medium: slightly behind (<75% required rate)
        if (rateRatio < MediumRateRatio)
        {
            return RuleSeverity.Medium;
        }

        return RuleSeverity.Low;
    }

    private static decimal ComputeScore(decimal rateRatio, int daysRemaining, int priority)
    {
        // Base score from rate ratio (lower ratio = higher score)
        var baseScore = rateRatio switch
        {
            < CriticalRateRatio => 0.90m,
            < HighRateRatio => 0.80m,
            < MediumRateRatio => 0.70m,
            _ => 0.60m
        };

        // Priority bonus
        var priorityBonus = priority switch
        {
            1 => 0.05m,
            2 => 0.02m,
            _ => 0.0m
        };

        // Urgency bonus for approaching deadlines
        var urgencyBonus = daysRemaining switch
        {
            <= CriticalDaysRemaining => 0.05m,
            <= HighDaysRemaining => 0.02m,
            _ => 0.0m
        };

        return Math.Min(baseScore + priorityBonus + urgencyBonus, 0.95m);
    }

    private static string BuildRationale(GoalProgressAnalysis analysis, RuleSeverity severity)
    {
        var progressPercent = Math.Round(analysis.CurrentProgress * 100);
        var projectedPercent = Math.Round(analysis.ProjectedCompletion * 100);

        return severity switch
        {
            RuleSeverity.Critical =>
                $"At your current pace, you'll reach only {projectedPercent}% by the deadline. " +
                $"With {analysis.DaysRemaining} days left, you need to significantly accelerate progress on lead metrics. " +
                "Consider focusing exclusively on this goal today.",

            RuleSeverity.High =>
                $"You're at {progressPercent}% with {analysis.DaysRemaining} days remaining. " +
                $"Current trajectory projects {projectedPercent}% completion. " +
                "Review your lead metrics and identify what's blocking faster progress.",

            _ =>
                $"Progress on this goal ({progressPercent}%) is slightly behind schedule. " +
                $"With {analysis.DaysRemaining} days left, consider whether your lead activities are sufficient."
        };
    }

    private sealed record GoalProgressAnalysis(
        GoalSnapshot Goal,
        int DaysRemaining,
        decimal CurrentProgress,
        decimal RequiredDailyRate,
        decimal ActualDailyRate,
        decimal RateRatio,
        decimal ProjectedCompletion,
        bool IsAtRisk);
}

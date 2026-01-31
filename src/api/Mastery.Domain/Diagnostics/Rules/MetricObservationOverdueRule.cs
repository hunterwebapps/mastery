using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Entities.Metrics;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;

namespace Mastery.Domain.Diagnostics.Rules;

/// <summary>
/// Detects manual metrics that haven't been recorded within their expected observation cadence.
/// Triggers when: Metric with SourceType=Manual hasn't been observed within expected window based on DefaultCadence.
/// </summary>
public sealed class MetricObservationOverdueRule : DeterministicRuleBase
{
    private const int DailyGraceDays = 2;      // Allow 2 days grace for daily metrics
    private const int WeeklyGraceDays = 3;     // Allow 3 days grace for weekly metrics
    private const int MonthlyGraceDays = 7;    // Allow 7 days grace for monthly metrics
    private const int CriticalOverdueDays = 14; // 2 weeks overdue is critical

    public override string RuleId => "METRIC_OBSERVATION_OVERDUE";
    public override string RuleName => "Metric Observation Overdue";
    public override string Description => "Detects manual metrics that haven't been recorded within their expected observation cadence.";

    public override Task<RuleResult> EvaluateAsync(
        UserStateSnapshot state,
        IReadOnlyList<SignalEntry> signals,
        CancellationToken ct = default)
    {
        // Early exit if no relevant signals in the batch
        var relevantSignals = signals.Where(s =>
            s.EventType == nameof(MetricObservationRecordedEvent) ||
            s.EventType == nameof(MetricDefinitionCreatedEvent) ||
            s.EventType == "MorningWindowStart").ToList();

        if (relevantSignals.Count == 0)
            return Task.FromResult(NotTriggered());

        var overdueMetrics = new List<MetricOverdueAnalysis>();

        foreach (var metric in state.MetricDefinitions.Where(m =>
            m.SourceType == MetricSourceType.Manual &&
            !string.IsNullOrEmpty(m.DefaultCadence)))
        {
            var expectedDays = GetExpectedObservationDays(metric.DefaultCadence);
            if (expectedDays <= 0)
                continue;

            var daysSinceObservation = metric.LastObservationDate.HasValue
                ? (int)(state.Today.ToDateTime(TimeOnly.MinValue) - metric.LastObservationDate.Value).TotalDays
                : int.MaxValue; // Never observed

            var graceDays = GetGraceDays(metric.DefaultCadence);
            var thresholdDays = expectedDays + graceDays;

            if (daysSinceObservation > thresholdDays)
            {
                // Check if this metric is bound to any active goals
                var linkedGoals = state.Goals
                    .Where(g => g.Status == GoalStatus.Active &&
                                g.Metrics.Any(m => m.MetricDefinitionId == metric.Id))
                    .ToList();

                var highestLinkedPriority = linkedGoals.Count > 0
                    ? linkedGoals.Min(g => g.Priority)
                    : (int?)null;

                overdueMetrics.Add(new MetricOverdueAnalysis(
                    Metric: metric,
                    DaysSinceObservation: daysSinceObservation == int.MaxValue ? -1 : daysSinceObservation,
                    ExpectedDays: expectedDays,
                    ThresholdDays: thresholdDays,
                    DaysOverdue: daysSinceObservation - thresholdDays,
                    NeverObserved: !metric.LastObservationDate.HasValue,
                    LinkedGoalCount: linkedGoals.Count,
                    HighestLinkedPriority: highestLinkedPriority));
            }
        }

        if (overdueMetrics.Count == 0)
            return Task.FromResult(NotTriggered());

        // Prioritize by: linked to P1/P2 goal, then days overdue
        var mostOverdue = overdueMetrics
            .OrderBy(m => m.HighestLinkedPriority ?? 999)
            .ThenByDescending(m => m.DaysOverdue)
            .First();

        var severity = ComputeSeverity(
            mostOverdue.DaysOverdue,
            mostOverdue.NeverObserved,
            mostOverdue.HighestLinkedPriority);

        var evidence = new Dictionary<string, object>
        {
            ["OverdueMetricCount"] = overdueMetrics.Count,
            ["MostOverdueMetricId"] = mostOverdue.Metric.Id,
            ["MostOverdueMetricName"] = mostOverdue.Metric.Name,
            ["Cadence"] = mostOverdue.Metric.DefaultCadence,
            ["DaysSinceObservation"] = mostOverdue.DaysSinceObservation,
            ["ExpectedDays"] = mostOverdue.ExpectedDays,
            ["DaysOverdue"] = mostOverdue.DaysOverdue,
            ["NeverObserved"] = mostOverdue.NeverObserved,
            ["LinkedGoalCount"] = mostOverdue.LinkedGoalCount,
            ["HighestLinkedPriority"] = mostOverdue.HighestLinkedPriority ?? (object)"None",
            ["AllOverdueMetrics"] = overdueMetrics.Select(m => new
            {
                m.Metric.Id,
                m.Metric.Name,
                m.Metric.DefaultCadence,
                m.DaysSinceObservation,
                m.DaysOverdue,
                m.NeverObserved,
                m.LinkedGoalCount
            }).ToList()
        };

        var title = mostOverdue.NeverObserved
            ? $"Record your first \"{mostOverdue.Metric.Name}\" observation"
            : $"\"{mostOverdue.Metric.Name}\" needs an update ({mostOverdue.Metric.DefaultCadence.ToLower()} metric)";

        var rationale = BuildRationale(mostOverdue);

        var score = ComputeScore(mostOverdue.DaysOverdue, mostOverdue.NeverObserved, mostOverdue.HighestLinkedPriority);

        var directRecommendation = new DirectRecommendationCandidate(
            Type: RecommendationType.MetricObservationReminder,
            Context: RecommendationContext.ProactiveCheck,
            TargetKind: RecommendationTargetKind.Metric,
            TargetEntityId: mostOverdue.Metric.Id,
            TargetEntityTitle: mostOverdue.Metric.Name,
            ActionKind: RecommendationActionKind.Update,
            Title: title,
            Rationale: rationale,
            Score: score,
            ActionSummary: "Record metric observation");

        return Task.FromResult(Triggered(severity, evidence, directRecommendation));
    }

    private static int GetExpectedObservationDays(string cadence)
    {
        return cadence.ToLowerInvariant() switch
        {
            "daily" => 1,
            "weekly" => 7,
            "monthly" => 30,
            "biweekly" => 14,
            "quarterly" => 90,
            _ => 7 // Default to weekly if unknown
        };
    }

    private static int GetGraceDays(string cadence)
    {
        return cadence.ToLowerInvariant() switch
        {
            "daily" => DailyGraceDays,
            "weekly" => WeeklyGraceDays,
            "monthly" => MonthlyGraceDays,
            "biweekly" => WeeklyGraceDays,
            "quarterly" => MonthlyGraceDays,
            _ => WeeklyGraceDays
        };
    }

    private static RuleSeverity ComputeSeverity(int daysOverdue, bool neverObserved, int? linkedPriority)
    {
        // Critical: significantly overdue (2+ weeks) OR linked to P1 goal and overdue
        if (daysOverdue >= CriticalOverdueDays || (linkedPriority == 1 && daysOverdue > 7))
        {
            return RuleSeverity.Critical;
        }

        // High: linked to P1/P2 goal OR moderately overdue
        if (linkedPriority <= 2 || daysOverdue >= 7 || neverObserved)
        {
            return RuleSeverity.High;
        }

        // Medium: slightly overdue
        if (daysOverdue >= 3)
        {
            return RuleSeverity.Medium;
        }

        return RuleSeverity.Low;
    }

    private static decimal ComputeScore(int daysOverdue, bool neverObserved, int? linkedPriority)
    {
        // Base score from days overdue
        var baseScore = daysOverdue switch
        {
            >= CriticalOverdueDays => 0.80m,
            >= 7 => 0.70m,
            >= 3 => 0.60m,
            _ => 0.50m
        };

        // Bonus if never observed (first observation is important)
        if (neverObserved)
            baseScore = Math.Max(baseScore, 0.65m);

        // Priority bonus for linked goals
        var priorityBonus = linkedPriority switch
        {
            1 => 0.10m,
            2 => 0.05m,
            _ => 0.0m
        };

        return Math.Min(baseScore + priorityBonus, 0.90m);
    }

    private static string BuildRationale(MetricOverdueAnalysis analysis)
    {
        var cadence = analysis.Metric.DefaultCadence.ToLowerInvariant();
        var goalNote = analysis.LinkedGoalCount > 0
            ? $" This metric is linked to {analysis.LinkedGoalCount} goal(s), so keeping it updated helps track progress accurately."
            : "";

        if (analysis.NeverObserved)
        {
            return $"This {cadence} metric has never been recorded. Recording an initial observation establishes your baseline for tracking progress.{goalNote}";
        }

        return analysis.DaysOverdue switch
        {
            >= CriticalOverdueDays =>
                $"This {cadence} metric hasn't been updated in {analysis.DaysSinceObservation} days. " +
                $"Without recent data, it's hard to know if you're on track.{goalNote}",

            >= 7 =>
                $"Your last observation was {analysis.DaysSinceObservation} days ago. " +
                $"As a {cadence} metric, consider recording a new observation to keep your tracking accurate.{goalNote}",

            _ =>
                $"This {cadence} metric is due for an update (last recorded {analysis.DaysSinceObservation} days ago).{goalNote}"
        };
    }

    private sealed record MetricOverdueAnalysis(
        MetricDefinitionSnapshot Metric,
        int DaysSinceObservation,
        int ExpectedDays,
        int ThresholdDays,
        int DaysOverdue,
        bool NeverObserved,
        int LinkedGoalCount,
        int? HighestLinkedPriority);
}

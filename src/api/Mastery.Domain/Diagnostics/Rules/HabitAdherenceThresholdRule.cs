using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;

namespace Mastery.Domain.Diagnostics.Rules;

/// <summary>
/// Detects habits with adherence dropping below acceptable thresholds.
/// Triggers when: 7-day adherence < 50% for any active habit.
/// </summary>
public sealed class HabitAdherenceThresholdRule : DeterministicRuleBase
{
    private const decimal CriticalThreshold = 0.25m; // 25%
    private const decimal WarningThreshold = 0.50m;  // 50%
    private const int LongStreakThreshold = 14;      // 2+ weeks
    private const int MediumStreakThreshold = 7;     // 1+ week
    private const decimal SystemicIssueThreshold = 0.50m; // >50% habits struggling

    public override string RuleId => "HABIT_ADHERENCE_THRESHOLD";
    public override string RuleName => "Habit Adherence Threshold";
    public override string Description => "Detects habits with adherence dropping below 50% over the past 7 days.";

    public override Task<RuleResult> EvaluateAsync(
        UserStateSnapshot state,
        IReadOnlyList<SignalEntry> signals,
        CancellationToken ct = default)
    {
        var activeHabits = state.Habits.Where(h => h.Status == HabitStatus.Active).ToList();

        var strugglingHabits = activeHabits
            .Where(h => h.Adherence7Day < WarningThreshold)
            .OrderBy(h => h.Adherence7Day)
            .ToList();

        if (strugglingHabits.Count == 0)
            return Task.FromResult(NotTriggered());

        var worst = strugglingHabits.First();

        // Check if worst habit is linked to a P1/P2 goal
        var linkedGoalPriorities = state.Goals
            .Where(g => worst.GoalIds?.Contains(g.Id) == true)
            .Select(g => g.Priority)
            .ToList();
        var isLinkedToHighPriorityGoal = linkedGoalPriorities.Any(p => p <= 2);

        // Multi-factor severity: adherence level + streak + mode + goal priority
        var severity = ComputeSeverity(
            worst.Adherence7Day,
            worst.CurrentStreak,
            worst.CurrentMode,
            isLinkedToHighPriorityGoal);

        // Volume context: systemic issue detection
        var totalActiveHabits = activeHabits.Count;
        var percentageStrugglingHabits = totalActiveHabits > 0
            ? (decimal)strugglingHabits.Count / totalActiveHabits
            : 0m;
        var requiresEscalation = percentageStrugglingHabits > SystemicIssueThreshold;

        // Mode-aware scoring: habits at Minimum have nowhere to scale down
        var modeBonus = worst.CurrentMode switch
        {
            HabitMode.Full => 0.00m,        // Most scalable
            HabitMode.Maintenance => 0.05m, // Some room
            HabitMode.Minimum => 0.10m,     // Nowhere to go - urgent
            _ => 0.00m
        };
        var score = Math.Min(0.85m + modeBonus, 0.95m);

        var evidence = new Dictionary<string, object>
        {
            ["StrugglingHabitCount"] = strugglingHabits.Count,
            ["TotalActiveHabits"] = totalActiveHabits,
            ["PercentageStrugglingHabits"] = Math.Round(percentageStrugglingHabits * 100, 1),
            ["WorstAdherence"] = Math.Round(worst.Adherence7Day * 100, 1),
            ["WorstHabitId"] = worst.Id,
            ["WorstHabitTitle"] = worst.Title,
            ["WorstHabitMode"] = worst.CurrentMode.ToString(),
            ["WorstHabitStreak"] = worst.CurrentStreak,
            ["LinkedToHighPriorityGoal"] = isLinkedToHighPriorityGoal,
            ["AllStrugglingHabits"] = strugglingHabits.Select(h => new
            {
                h.Id,
                h.Title,
                Adherence = Math.Round(h.Adherence7Day * 100, 1),
                h.CurrentMode,
                h.CurrentStreak
            }).ToList()
        };

        // Suggest scaling down if not already at minimum
        var actionKind = worst.CurrentMode == HabitMode.Minimum
            ? RecommendationActionKind.ReflectPrompt
            : RecommendationActionKind.Update;

        var title = worst.CurrentMode == HabitMode.Minimum
            ? $"\"{worst.Title}\" needs attention ({Math.Round(worst.Adherence7Day * 100)}% this week)"
            : $"Consider scaling down \"{worst.Title}\" ({Math.Round(worst.Adherence7Day * 100)}% adherence)";

        var rationale = worst.CurrentMode == HabitMode.Minimum
            ? $"Even at minimum mode, you're struggling with this habit. Consider if it's the right time for this habit, or if there's an obstacle to address."
            : $"Your adherence to \"{worst.Title}\" has dropped to {Math.Round(worst.Adherence7Day * 100)}%. Switching to minimum mode might help you maintain consistency while you rebuild momentum.";

        var directRecommendation = new DirectRecommendationCandidate(
            Type: RecommendationType.HabitModeSuggestion,
            Context: RecommendationContext.DriftAlert,
            TargetKind: RecommendationTargetKind.Habit,
            TargetEntityId: worst.Id,
            TargetEntityTitle: worst.Title,
            ActionKind: actionKind,
            Title: title,
            Rationale: rationale,
            Score: score,
            ActionSummary: worst.CurrentMode == HabitMode.Minimum
                ? "Reflect on blockers"
                : "Scale to minimum mode");

        return Task.FromResult(Triggered(severity, evidence, directRecommendation, requiresEscalation));
    }

    /// <summary>
    /// Computes severity using multiple factors: adherence level, streak length, current mode, and goal linkage.
    /// Long streaks at risk are more urgent (psychological investment). Minimum mode with low adherence
    /// is more urgent (no further scaling possible). P1/P2 goal linkage boosts severity.
    /// </summary>
    private static RuleSeverity ComputeSeverity(
        decimal adherence,
        int streak,
        HabitMode mode,
        bool linkedToHighPriorityGoal)
    {
        // Base severity from adherence + streak
        var baseSeverity = (adherence, streak) switch
        {
            (<= CriticalThreshold, >= LongStreakThreshold) => RuleSeverity.Critical,  // Protect long streaks
            (<= CriticalThreshold, _) => RuleSeverity.High,
            (<= WarningThreshold, >= LongStreakThreshold) => RuleSeverity.High,
            (<= WarningThreshold, >= MediumStreakThreshold) => RuleSeverity.Medium,
            _ => RuleSeverity.Low
        };

        // Boost severity if at Minimum mode (can't scale down further)
        if (mode == HabitMode.Minimum && baseSeverity == RuleSeverity.Medium)
            baseSeverity = RuleSeverity.High;

        // Boost severity if linked to P1/P2 goal
        if (linkedToHighPriorityGoal && baseSeverity < RuleSeverity.High)
            baseSeverity = RuleSeverity.High;

        return baseSeverity;
    }
}

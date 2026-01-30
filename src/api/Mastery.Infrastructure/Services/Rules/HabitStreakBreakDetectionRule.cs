using Mastery.Application.Common.Models;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;

namespace Mastery.Infrastructure.Services.Rules;

/// <summary>
/// Detects habits with active streaks that are at risk of breaking today.
/// Triggers when: habit is scheduled today, not yet completed, and has a streak > 0.
/// </summary>
public sealed class HabitStreakBreakDetectionRule : DeterministicRuleBase
{
    private const int HighValueStreakThreshold = 7;
    private const int CriticalStreakThreshold = 21;

    public override string RuleId => "HABIT_STREAK_BREAK_RISK";
    public override string RuleName => "Habit Streak Break Detection";
    public override string Description => "Detects habits with valuable streaks that are at risk of breaking today.";

    public override Task<RuleResult> EvaluateAsync(
        UserStateSnapshot state,
        IReadOnlyList<SignalEntry> signals,
        CancellationToken ct = default)
    {
        var atRiskHabits = new List<(Guid Id, string Title, int CurrentStreak, bool IsScheduledToday)>();

        foreach (var habit in state.Habits.Where(h =>
            h.Status == HabitStatus.Active &&
            h.CurrentStreak > 0))
        {
            var isScheduledToday = IsHabitScheduledToday(habit, state.Today);

            if (isScheduledToday)
            {
                // Check if there's evidence it's been completed today
                // by looking for relevant signals
                var hasCompletedSignal = signals.Any(s =>
                    s.EventType == "HabitCompleted" &&
                    s.TargetEntityId == habit.Id);

                if (!hasCompletedSignal)
                {
                    atRiskHabits.Add((habit.Id, habit.Title, habit.CurrentStreak, isScheduledToday));
                }
            }
        }

        if (atRiskHabits.Count == 0)
            return Task.FromResult(NotTriggered());

        // Find the habit with the highest streak at risk
        var mostValuable = atRiskHabits.MaxBy(h => h.CurrentStreak)!;

        var severity = mostValuable.CurrentStreak switch
        {
            >= CriticalStreakThreshold => RuleSeverity.Critical,
            >= HighValueStreakThreshold => RuleSeverity.High,
            >= 3 => RuleSeverity.Medium,
            _ => RuleSeverity.Low
        };

        var evidence = new Dictionary<string, object>
        {
            ["AtRiskHabitCount"] = atRiskHabits.Count,
            ["HighestStreakAtRisk"] = mostValuable.CurrentStreak,
            ["MostValuableHabitId"] = mostValuable.Id,
            ["MostValuableHabitTitle"] = mostValuable.Title,
            ["AllAtRiskHabits"] = atRiskHabits.Select(h => new { h.Id, h.Title, h.CurrentStreak }).ToList()
        };

        var directRecommendation = new DirectRecommendationCandidate(
            Type: RecommendationType.NextBestAction,
            Context: RecommendationContext.DriftAlert,
            TargetKind: RecommendationTargetKind.Habit,
            TargetEntityId: mostValuable.Id,
            TargetEntityTitle: mostValuable.Title,
            ActionKind: RecommendationActionKind.ExecuteToday,
            Title: $"Don't break your {mostValuable.CurrentStreak}-day streak on \"{mostValuable.Title}\"",
            Rationale: $"You've maintained this habit for {mostValuable.CurrentStreak} consecutive days. Missing today would reset your progress. Even a minimum version counts!",
            Score: Math.Min(0.5m + (mostValuable.CurrentStreak * 0.02m), 0.95m),
            ActionSummary: "Complete habit (even minimum version)");

        return Task.FromResult(Triggered(severity, evidence, directRecommendation));
    }

    private static bool IsHabitScheduledToday(HabitSnapshot habit, DateOnly today)
    {
        if (habit.Schedule == null)
            return true; // Default to daily if no schedule

        return habit.Schedule.Type.ToLowerInvariant() switch
        {
            "daily" => true,
            "weekly" when habit.Schedule.DaysOfWeek != null =>
                habit.Schedule.DaysOfWeek.Contains((int)today.DayOfWeek),
            "interval" when habit.Schedule.IntervalDays.HasValue =>
                // Would need last completion date to calculate properly
                // For now, assume scheduled
                true,
            _ => true
        };
    }
}

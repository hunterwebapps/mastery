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
        // Pre-compute completion signals by habit for O(1) lookup
        var completionsByHabit = signals
            .Where(s => s.EventType == "HabitCompleted" && s.TargetEntityId.HasValue)
            .GroupBy(s => s.TargetEntityId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(s => s.CreatedAt).ToList());

        var atRiskHabits = new List<(Guid Id, string Title, int CurrentStreak)>();

        foreach (var habit in state.Habits.Where(h =>
            h.Status == HabitStatus.Active &&
            h.CurrentStreak > 0))
        {
            completionsByHabit.TryGetValue(habit.Id, out var habitCompletions);
            var isScheduledToday = IsHabitScheduledToday(habit, state.Today, habitCompletions);

            if (isScheduledToday)
            {
                // Check if completed today by looking at the most recent completion
                var completedToday = habitCompletions?.Any(s =>
                    DateOnly.FromDateTime(s.CreatedAt) == state.Today) ?? false;

                if (!completedToday)
                {
                    atRiskHabits.Add((habit.Id, habit.Title, habit.CurrentStreak));
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
            ["AllAtRiskHabits"] = atRiskHabits
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

    private static bool IsHabitScheduledToday(
        HabitSnapshot habit,
        DateOnly today,
        IReadOnlyList<SignalEntry>? completions)
    {
        if (habit.Schedule == null)
            return true; // Default to daily if no schedule

        var scheduleType = habit.Schedule.Type;

        if (scheduleType.Equals("daily", StringComparison.OrdinalIgnoreCase))
            return true;

        if (scheduleType.Equals("weekly", StringComparison.OrdinalIgnoreCase))
            return habit.Schedule.DaysOfWeek?.Contains((int)today.DayOfWeek) ?? true;

        if (scheduleType.Equals("interval", StringComparison.OrdinalIgnoreCase))
            return IsIntervalScheduledToday(habit.Schedule.IntervalDays, today, completions);

        return true; // Unknown schedule type, assume scheduled
    }

    private static bool IsIntervalScheduledToday(
        int? intervalDays,
        DateOnly today,
        IReadOnlyList<SignalEntry>? completions)
    {
        if (!intervalDays.HasValue || intervalDays.Value <= 0)
            return true; // Invalid interval, assume scheduled

        if (completions == null || completions.Count == 0)
            return true; // No completion history, assume scheduled (conservative)

        // Find the most recent completion date
        var lastCompletionDate = DateOnly.FromDateTime(completions[0].CreatedAt);

        var daysSinceCompletion = today.DayNumber - lastCompletionDate.DayNumber;
        return daysSinceCompletion >= intervalDays.Value;
    }
}

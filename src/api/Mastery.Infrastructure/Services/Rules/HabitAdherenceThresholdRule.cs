using Mastery.Application.Common.Models;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;

namespace Mastery.Infrastructure.Services.Rules;

/// <summary>
/// Detects habits with adherence dropping below acceptable thresholds.
/// Triggers when: 7-day adherence < 50% for any active habit.
/// </summary>
public sealed class HabitAdherenceThresholdRule : DeterministicRuleBase
{
    private const decimal CriticalThreshold = 0.25m; // 25%
    private const decimal WarningThreshold = 0.50m;  // 50%
    private const decimal CautionThreshold = 0.70m;  // 70%

    public override string RuleId => "HABIT_ADHERENCE_THRESHOLD";
    public override string RuleName => "Habit Adherence Threshold";
    public override string Description => "Detects habits with adherence dropping below 50% over the past 7 days.";

    public override Task<RuleResult> EvaluateAsync(
        UserStateSnapshot state,
        IReadOnlyList<SignalEntry> signals,
        CancellationToken ct = default)
    {
        var strugglingHabits = state.Habits
            .Where(h => h.Status == HabitStatus.Active
                        && h.Adherence7Day < WarningThreshold)
            .OrderBy(h => h.Adherence7Day)
            .ToList();

        if (strugglingHabits.Count == 0)
            return Task.FromResult(NotTriggered());

        var worst = strugglingHabits.First();

        var severity = worst.Adherence7Day switch
        {
            <= CriticalThreshold => RuleSeverity.High,
            <= WarningThreshold => RuleSeverity.Medium,
            _ => RuleSeverity.Low
        };

        var evidence = new Dictionary<string, object>
        {
            ["StrugglingHabitCount"] = strugglingHabits.Count,
            ["WorstAdherence"] = Math.Round(worst.Adherence7Day * 100, 1),
            ["WorstHabitId"] = worst.Id,
            ["WorstHabitTitle"] = worst.Title,
            ["WorstHabitMode"] = worst.CurrentMode.ToString(),
            ["AllStrugglingHabits"] = strugglingHabits.Select(h => new
            {
                h.Id,
                h.Title,
                Adherence = Math.Round(h.Adherence7Day * 100, 1),
                h.CurrentMode
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
            Score: 0.85m,
            ActionSummary: worst.CurrentMode == HabitMode.Minimum
                ? "Reflect on blockers"
                : "Scale to minimum mode");

        return Task.FromResult(Triggered(severity, evidence, directRecommendation));
    }
}

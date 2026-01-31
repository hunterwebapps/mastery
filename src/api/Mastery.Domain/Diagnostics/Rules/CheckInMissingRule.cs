using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;

namespace Mastery.Domain.Diagnostics.Rules;

/// <summary>
/// Detects when expected check-ins are missing for the current day.
/// Triggers based on time of day and user's check-in schedule.
/// </summary>
public sealed class CheckInMissingRule : DeterministicRuleBase
{
    public override string RuleId => "CHECKIN_MISSING";
    public override string RuleName => "Missing Check-In Detection";
    public override string Description => "Detects when morning or evening check-ins are missing and overdue.";

    public override Task<RuleResult> EvaluateAsync(
        UserStateSnapshot state,
        IReadOnlyList<SignalEntry> signals,
        CancellationToken ct = default)
    {
        // Note: This rule is designed to be triggered by a scheduled signal,
        // not by evaluating against a specific time. The signal scheduler
        // determines when to check based on user preferences.

        var todayCheckIns = state.RecentCheckIns
            .Where(c => c.Date == state.Today)
            .ToList();

        var hasMorning = todayCheckIns.Any(c => c.Type == CheckInType.Morning);
        var hasEvening = todayCheckIns.Any(c => c.Type == CheckInType.Evening);

        // Check for window start signals (explicit morning/evening only)
        var reminderSignal = signals.FirstOrDefault(s =>
            s.EventType == "MorningWindowStart" ||
            s.EventType == "EveningWindowStart");

        if (reminderSignal == null)
            return Task.FromResult(NotTriggered());

        var expectedType = reminderSignal.EventType.ToLower().Contains("morning")
            ? CheckInType.Morning
            : CheckInType.Evening;

        var isMissing = expectedType == CheckInType.Morning ? !hasMorning : !hasEvening;

        if (!isMissing)
            return Task.FromResult(NotTriggered());

        // Calculate severity based on streak (protect longer streaks more aggressively)
        var severity = state.CheckInStreak switch
        {
            >= 30 => RuleSeverity.Critical, // Protect 30+ day streaks
            >= 14 => RuleSeverity.High,     // Long streak at risk
            >= 7 => RuleSeverity.Medium,
            _ => RuleSeverity.Low
        };

        var checkInTypeName = expectedType == CheckInType.Morning ? "morning" : "evening";

        var evidence = new Dictionary<string, object>
        {
            ["ExpectedCheckInType"] = expectedType.ToString(),
            ["CurrentStreak"] = state.CheckInStreak,
            ["HasMorningCheckIn"] = hasMorning,
            ["HasEveningCheckIn"] = hasEvening,
            ["SignalType"] = reminderSignal.EventType,
            ["SignalScheduledAt"] = reminderSignal.ScheduledWindowStart?.ToString("o") ?? "N/A"
        };

        var directRecommendation = new DirectRecommendationCandidate(
            Type: RecommendationType.CheckInConsistencyNudge,
            Context: expectedType == CheckInType.Morning
                ? RecommendationContext.MorningCheckIn
                : RecommendationContext.EveningCheckIn,
            TargetKind: RecommendationTargetKind.UserProfile,
            TargetEntityId: null,
            TargetEntityTitle: null,
            ActionKind: RecommendationActionKind.ReflectPrompt,
            Title: state.CheckInStreak > 0
                ? $"Don't break your {state.CheckInStreak}-day check-in streak"
                : $"Time for your {checkInTypeName} check-in",
            Rationale: state.CheckInStreak > 0
                ? $"You've checked in consistently for {state.CheckInStreak} days. A quick {checkInTypeName} check-in keeps your streak alive and helps you stay on track."
                : $"A brief {checkInTypeName} check-in helps you set intentions and track progress. It only takes a minute.",
            Score: Math.Min(0.50m + (state.CheckInStreak * 0.015m), 0.90m), // Linear scaling: 0 days → 0.50, 7 days → 0.61, 14 days → 0.71, 30 days → 0.90
            ActionSummary: $"Complete {checkInTypeName} check-in");

        return Task.FromResult(Triggered(severity, evidence, directRecommendation));
    }
}

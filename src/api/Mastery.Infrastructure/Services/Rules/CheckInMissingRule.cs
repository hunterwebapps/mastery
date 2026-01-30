using Mastery.Application.Common.Models;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;

namespace Mastery.Infrastructure.Services.Rules;

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

        // Check if there's a "CheckInReminderDue" signal
        var reminderSignal = signals.FirstOrDefault(s =>
            s.EventType == "CheckInReminderDue" ||
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

        // Calculate severity based on streak
        var severity = state.CheckInStreak switch
        {
            >= 14 => RuleSeverity.High, // Long streak at risk
            >= 7 => RuleSeverity.Medium,
            >= 3 => RuleSeverity.Low,
            _ => RuleSeverity.Low
        };

        var checkInTypeName = expectedType == CheckInType.Morning ? "morning" : "evening";

        var evidence = new Dictionary<string, object>
        {
            ["ExpectedCheckInType"] = expectedType.ToString(),
            ["CurrentStreak"] = state.CheckInStreak,
            ["HasMorningCheckIn"] = hasMorning,
            ["HasEveningCheckIn"] = hasEvening,
            ["SignalType"] = reminderSignal.EventType
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
            Score: state.CheckInStreak > 7 ? 0.8m : 0.6m,
            ActionSummary: $"Complete {checkInTypeName} check-in");

        return Task.FromResult(Triggered(severity, evidence, directRecommendation));
    }
}

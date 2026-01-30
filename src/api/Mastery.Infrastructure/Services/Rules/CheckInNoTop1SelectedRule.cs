using Mastery.Application.Common.Models;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;

namespace Mastery.Infrastructure.Services.Rules;

/// <summary>
/// Detects when a morning check-in was submitted without selecting a Top 1 priority.
/// The Top 1 is a core element of the daily planning loop.
/// </summary>
public sealed class CheckInNoTop1SelectedRule : DeterministicRuleBase
{
    public override string RuleId => "CHECKIN_NO_TOP1_SELECTED";
    public override string RuleName => "Missing Top 1 Selection";
    public override string Description => "Detects morning check-ins without a Top 1 priority, which is critical for daily focus.";

    public override Task<RuleResult> EvaluateAsync(
        UserStateSnapshot state,
        IReadOnlyList<SignalEntry> signals,
        CancellationToken ct = default)
    {
        // Find today's completed morning check-in
        var todayMorningCheckIn = state.RecentCheckIns
            .FirstOrDefault(c => c.Date == state.Today && c.Type == CheckInType.Morning);

        // Rule only evaluates completed check-ins (draft may still be in progress)
        if (todayMorningCheckIn == null || todayMorningCheckIn.Status != CheckInStatus.Completed)
            return Task.FromResult(NotTriggered());

        // Top1Type is the canonical indicator of selection - if set, user made a selection
        if (!string.IsNullOrEmpty(todayMorningCheckIn.Top1Type))
            return Task.FromResult(NotTriggered());

        // Check if there was a recent check-in submitted signal for context
        var checkInSignal = signals.FirstOrDefault(s =>
            s.EventType == "CheckInSubmitted" &&
            s.TargetEntityId == todayMorningCheckIn.Id);

        var evidence = new Dictionary<string, object>
        {
            ["CheckInId"] = todayMorningCheckIn.Id,
            ["CheckInDate"] = state.Today.ToString("yyyy-MM-dd"),
            ["CheckInStatus"] = todayMorningCheckIn.Status.ToString(),
            ["EnergyLevel"] = todayMorningCheckIn.EnergyLevel ?? 0,
            ["Top1Type"] = todayMorningCheckIn.Top1Type ?? "null",
            ["Top1EntityId"] = todayMorningCheckIn.Top1EntityId?.ToString() ?? "null",
            ["HasRecentSignal"] = checkInSignal != null
        };

        // Calculate severity based on check-in streak and user engagement
        var severity = state.CheckInStreak switch
        {
            >= 7 => RuleSeverity.Medium, // Engaged user, nudge gently
            >= 3 => RuleSeverity.Low,
            _ => RuleSeverity.Low // New user, very gentle
        };

        var directRecommendation = new DirectRecommendationCandidate(
            Type: RecommendationType.Top1Suggestion,
            Context: RecommendationContext.MorningCheckIn,
            TargetKind: RecommendationTargetKind.UserProfile,
            TargetEntityId: null,
            TargetEntityTitle: null,
            ActionKind: RecommendationActionKind.ReflectPrompt,
            Title: "Set your Top 1 priority for today",
            Rationale: "Starting your day with a clear Top 1 priority dramatically increases follow-through. What's the ONE thing that would make today a success?",
            Score: Math.Min(0.60m + (state.CheckInStreak * 0.02m), 0.85m),
            ActionSummary: "Select your most important task or goal");

        return Task.FromResult(Triggered(severity, evidence, directRecommendation));
    }
}

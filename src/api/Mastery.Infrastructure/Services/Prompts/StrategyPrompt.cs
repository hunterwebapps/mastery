using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mastery.Application.Common.Models;
using Mastery.Domain.Enums;

namespace Mastery.Infrastructure.Services.Prompts;

/// <summary>
/// Stage 2: Recommendation Strategy.
/// Given the situational assessment, decides what types of interventions are most impactful
/// and plans the generation stage.
/// </summary>
internal static class StrategyPrompt
{
    public const string PromptVersion = "strategy-v2.0";
    public const string Model = "gpt-5-mini";
    public const string SchemaName = "recommendation_strategy";

    public static readonly BinaryData ResponseSchema = BinaryData.FromString("""
        {
          "type": "object",
          "properties": {
            "maxRecommendations": { "type": "integer" },
            "interventionPlan": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "area": { "type": "string" },
                  "priority": { "type": "integer" },
                  "reasoning": { "type": "string" },
                  "targetType": { "type": "string" },
                  "targetEntityIds": {
                    "anyOf": [
                      { "type": "array", "items": { "type": "string" } },
                      { "type": "null" }
                    ]
                  }
                },
                "required": ["area", "priority", "reasoning", "targetType", "targetEntityIds"],
                "additionalProperties": false
              }
            }
          },
          "required": ["maxRecommendations", "interventionPlan"],
          "additionalProperties": false
        }
        """);

    private const string RecommendationTypes = """
        Valid recommendation types:
        - NextBestAction: suggest the most impactful task to work on now
        - Top1Suggestion: suggest the daily priority (Top-1) during morning check-in
        - HabitModeSuggestion: scale a habit to minimum mode when energy is low
        - PlanRealismAdjustment: defer tasks from an overloaded day
        - TaskBreakdownSuggestion: break a large or stuck task into subtasks
        - ScheduleAdjustmentSuggestion: move a task to a better day
        - ProjectStuckFix: define a next action for a stalled project
        - ExperimentRecommendation: suggest a behavioral experiment to address a pattern
        - GoalScoreboardSuggestion: add missing metrics to a goal's scoreboard
        - HabitFromLeadMetricSuggestion: create a new habit to drive an orphaned lead metric
        - CheckInConsistencyNudge: prompt the user to re-establish check-in routine
        - MetricObservationReminder: remind to record a stale manual metric
        """;

    public static string BuildSystemPrompt(RecommendationContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""
            You are a personal development strategist in the Mastery system.
            You receive a situational assessment of the user's current state.

            Your job is to create a strategic intervention plan:
            1. Decide which intervention areas are most impactful RIGHT NOW (not everything at once)
            2. Prioritize: what matters most given the user's current state?
            3. For each intervention, specify the targetType (recommendation type) and targetEntityIds (if applicable)
            4. Set a budget: how many total recommendations (3-7, never more than the user can act on)

            Principles:
            - Less is more: 3-5 focused recommendations beat 7 scattered ones
            - Capacity-aware: if the user is overloaded, don't pile on more tasks
            - Root cause over symptoms: address the underlying pattern, not just the surface signal
            - Energy-sensitive: low energy users need scaling down, not more ambition
            - One experiment at a time: never suggest more than one experiment per batch
            - Respect content boundaries: NEVER suggest interventions that violate stated boundaries
            - Match coaching style: Direct users want fewer words; Analytical users want more reasoning
            - Honor non-negotiables: Don't suggest anything that conflicts with season non-negotiables
            - Season-intensity-aware: High intensity seasons can accept more ambitious interventions; Recovery seasons need gentler approaches
            """);

        sb.AppendLine();
        sb.AppendLine(RecommendationTypes);
        sb.AppendLine();
        sb.AppendLine(GetContextInstructions(context));

        return sb.ToString();
    }

    public static string BuildUserPrompt(
        SituationalAssessment assessment,
        RecommendationContext context,
        UserProfileSnapshot? profile = null)
    {
        var sb = new StringBuilder();

        // Profile preferences and constraints at top for filtering decisions
        if (profile is not null)
        {
            sb.AppendLine("# User Preferences & Constraints");
            sb.AppendLine($"Coaching style: {profile.Preferences.CoachingStyle}");
            sb.AppendLine($"Explanation verbosity: {profile.Preferences.Verbosity}");
            sb.AppendLine($"Nudge level: {profile.Preferences.NudgeLevel}");
            sb.AppendLine($"Weekday capacity limit: {profile.Constraints.MaxPlannedMinutesWeekday}min");
            sb.AppendLine($"Weekend capacity limit: {profile.Constraints.MaxPlannedMinutesWeekend}min");
            if (profile.CurrentSeason is not null)
            {
                sb.AppendLine($"Season: {profile.CurrentSeason.Type} (intensity {profile.CurrentSeason.Intensity}/10)");
                if (profile.CurrentSeason.NonNegotiables.Count > 0)
                    sb.AppendLine($"Non-negotiables (MUST respect): {string.Join(", ", profile.CurrentSeason.NonNegotiables)}");
            }
            if (profile.Constraints.ContentBoundaries.Count > 0)
                sb.AppendLine($"Content boundaries (MUST respect): {string.Join(", ", profile.Constraints.ContentBoundaries)}");
            sb.AppendLine();
        }

        sb.AppendLine("# Situational Assessment (from Stage 1)");
        sb.AppendLine(JsonSerializer.Serialize(assessment, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }));
        sb.AppendLine();

        sb.AppendLine($"# Current Context: {context}");
        sb.AppendLine("Produce your strategic intervention plan.");

        return sb.ToString();
    }

    private static string GetContextInstructions(RecommendationContext context) => context switch
    {
        RecommendationContext.MorningCheckIn => """
            CONTEXT: Morning check-in.
            - Must include a Top1Suggestion or NextBestAction as top priority
            - Consider habit mode adjustments if energy is low
            - Plan realism check if day looks overloaded
            - Keep it to 3-5 recommendations maximum
            """,
        RecommendationContext.EveningCheckIn => """
            CONTEXT: Evening check-in.
            - Focus on rescheduling missed tasks (not guilt)
            - Suggest reflection prompts for patterns observed
            - Consider an experiment if a pattern has repeated 3+ times
            - Keep it to 3-4 recommendations maximum
            """,
        RecommendationContext.WeeklyReview => """
            CONTEXT: Weekly review.
            - Take the broadest view: goal momentum, habit trends, capacity patterns
            - Always suggest at least one experiment if patterns warrant it
            - Consider goal/metric adjustments for drifting goals
            - Up to 5-7 recommendations appropriate here
            """,
        RecommendationContext.DriftAlert => """
            CONTEXT: Drift alert.
            - Focus primarily on the drifting signal and its root cause
            - Be direct about what's off-track
            - Suggest concrete corrective action
            - Keep it to 2-3 focused recommendations
            """,
        RecommendationContext.Midday => """
            CONTEXT: Midday check.
            - Very brief: 1-2 recommendations maximum
            - Focus on next best action for remaining day
            - Adjust plan if morning went off-track
            """,
        RecommendationContext.Onboarding => """
            CONTEXT: Onboarding.
            - Suggest foundational elements: first habit, first metric to track
            - Start small: one keystone habit is better than five ambitious ones
            - 2-3 recommendations maximum
            """,
        RecommendationContext.ProactiveCheck => """
            CONTEXT: Proactive background check (system-initiated, not user-triggered).
            - Focus on the highest-leverage interventions the user hasn't addressed
            - Prioritize stuck projects, drifting metrics, and habit adherence drops
            - Consider experiments if patterns warrant investigation
            - Keep it to 3-5 recommendations maximum
            - Avoid duplicating recommendations the user already has pending
            """,
        _ => "CONTEXT: General. Produce 3-5 recommendations."
    };
}

// Stage 2 response model
internal sealed class RecommendationStrategy
{
    [JsonPropertyName("maxRecommendations")]
    public int MaxRecommendations { get; set; } = 5;

    [JsonPropertyName("interventionPlan")]
    public List<InterventionPlanItem> InterventionPlan { get; set; } = [];
}

internal sealed class InterventionPlanItem
{
    [JsonPropertyName("area")]
    public string Area { get; set; } = "";

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("reasoning")]
    public string Reasoning { get; set; } = "";

    [JsonPropertyName("targetType")]
    public string TargetType { get; set; } = "";

    [JsonPropertyName("targetEntityIds")]
    public List<string>? TargetEntityIds { get; set; }
}

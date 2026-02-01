using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mastery.Application.Common.Models;
using Mastery.Domain.Diagnostics;
using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Enums;
using Mastery.Infrastructure.Services.OpenAi.RAG;

namespace Mastery.Infrastructure.Services.OpenAi.Prompts;

/// <summary>
/// Stage 2: Candidate Selection.
/// Given the situational assessment and pre-computed candidates from Tier 0,
/// selects the most appropriate recommendations and provides personalized rationale.
/// </summary>
internal static class SelectionPrompt
{
    public const string PromptVersion = "selection-v1.0";
    public const string Model = "gpt-5-mini";
    public const string SchemaName = "recommendation_selection";

    public static readonly BinaryData ResponseSchema = BinaryData.FromString("""
        {
          "type": "object",
          "properties": {
            "selections": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "candidateIndex": { "type": "integer" },
                  "rationale": { "type": "string" },
                  "priorityRank": { "type": "integer" },
                  "refinedActionSummary": {
                    "anyOf": [
                      { "type": "string" },
                      { "type": "null" }
                    ]
                  }
                },
                "required": ["candidateIndex", "rationale", "priorityRank", "refinedActionSummary"],
                "additionalProperties": false
              }
            },
            "overallStrategy": { "type": "string" },
            "rejectedCandidatesReasoning": {
              "anyOf": [
                { "type": "string" },
                { "type": "null" }
              ]
            }
          },
          "required": ["selections", "overallStrategy", "rejectedCandidatesReasoning"],
          "additionalProperties": false
        }
        """);

    public static string BuildSystemPrompt(RecommendationContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""
            You are a personal development coach in the Mastery system.
            You receive a situational assessment and a list of PRE-COMPUTED recommendation candidates.

            Your job is to SELECT from these candidates — you CANNOT create new recommendations.
            For each selected candidate, provide a personalized rationale that:
            1. Explains WHY this recommendation matters for THIS user RIGHT NOW
            2. References specific details from the assessment (energy level, patterns, risks)
            3. Uses language matching the user's coaching style preference

            IMPORTANT CONSTRAINTS:
            - You can ONLY select from the provided candidates (by index)
            - You CANNOT modify the recommendation type, target entity, or action kind
            - You CAN refine the action summary to be more personalized
            - You CAN provide a custom rationale (replacing the generic one)
            - Select 3-5 candidates maximum (respect the context-specific limits below)
            - Rank selections by priority (1 = most important)

            SELECTION PRINCIPLES:
            - Less is more: 3-5 focused recommendations beat 7 scattered ones
            - Capacity-aware: if user is overloaded, prioritize scaling down over adding
            - Root cause: prefer recommendations that address underlying patterns
            - Energy-sensitive: low energy users need gentler interventions
            - Avoid conflicts: don't select multiple recommendations targeting the same entity
            - Respect boundaries: never select candidates that conflict with stated content boundaries

            REJECTION REASONING:
            - Briefly explain why you didn't select certain high-scoring candidates
            - This helps with explainability and debugging
            """);

        sb.AppendLine();
        sb.AppendLine(GetContextInstructions(context));

        return sb.ToString();
    }

    public static string BuildUserPrompt(
        SituationalAssessment assessment,
        IReadOnlyList<DirectRecommendationCandidate> candidates,
        RecommendationContext context,
        UserProfileSnapshot profile,
        RagContext? ragContext,
        DateOnly today)
    {
        var sb = new StringBuilder();

        // Profile preferences for personalization
        sb.AppendLine("# User Preferences");
        sb.AppendLine($"Coaching style: {profile.Preferences.CoachingStyle}");
        sb.AppendLine($"Verbosity: {profile.Preferences.Verbosity}");
        if (profile.CurrentSeason is not null)
        {
            sb.AppendLine($"Season: {profile.CurrentSeason.Type} (intensity {profile.CurrentSeason.Intensity}/10)");
            if (profile.CurrentSeason.NonNegotiables.Count > 0)
                sb.AppendLine($"Non-negotiables: {string.Join(", ", profile.CurrentSeason.NonNegotiables)}");
        }
        if (profile.Constraints.ContentBoundaries.Count > 0)
            sb.AppendLine($"Content boundaries (MUST respect): {string.Join(", ", profile.Constraints.ContentBoundaries)}");
        sb.AppendLine();

        // Historical context for personalization
        RagContextFormatter.AppendForSelection(sb, ragContext, today);

        // Assessment summary
        sb.AppendLine("# Situational Assessment");
        sb.AppendLine(JsonSerializer.Serialize(assessment, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }));
        sb.AppendLine();

        // Candidates list with indices
        sb.AppendLine("# Candidates (select by index)");
        sb.AppendLine("Each candidate has been scored by deterministic rules. Higher score = higher baseline priority.");
        sb.AppendLine();

        for (var i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            sb.AppendLine($"## Candidate {i}");
            sb.AppendLine($"Type: {c.Type}");
            sb.AppendLine($"Target: {c.TargetKind}{(c.TargetEntityId.HasValue ? $" ({c.TargetEntityId})" : "")}");
            if (!string.IsNullOrEmpty(c.TargetEntityTitle))
                sb.AppendLine($"Target Title: \"{c.TargetEntityTitle}\"");
            sb.AppendLine($"Action: {c.ActionKind}");
            sb.AppendLine($"Title: \"{c.Title}\"");
            sb.AppendLine($"Score: {c.Score:F2}");
            sb.AppendLine($"Default Rationale: \"{c.Rationale}\"");
            if (!string.IsNullOrEmpty(c.ActionSummary))
                sb.AppendLine($"Action Summary: \"{c.ActionSummary}\"");
            sb.AppendLine();
        }

        sb.AppendLine($"# Context: {context}");
        sb.AppendLine("Select the most appropriate candidates and provide personalized rationale for each.");

        return sb.ToString();
    }

    private static string GetContextInstructions(RecommendationContext context) => context switch
    {
        RecommendationContext.MorningCheckIn => """
            CONTEXT: Morning check-in.
            - Select 3-5 candidates maximum
            - Prioritize: Top1 or NextBestAction should be rank 1 if available
            - Consider habit mode adjustments if energy is low
            - Frame rationales around "today's focus"
            """,
        RecommendationContext.EveningCheckIn => """
            CONTEXT: Evening check-in.
            - Select 3-4 candidates maximum
            - Focus on rescheduling (not guilt) and reflection
            - Frame rationales around "setting up tomorrow for success"
            """,
        RecommendationContext.WeeklyReview => """
            CONTEXT: Weekly review.
            - Select up to 5-7 candidates
            - Include at least one experiment if patterns warrant it
            - Frame rationales around "this week's learnings" and "next week's focus"
            """,
        RecommendationContext.DriftAlert => """
            CONTEXT: Drift alert.
            - Select 2-3 candidates maximum
            - Prioritize the drifting signal and its root cause
            - Be direct and focused in rationales
            """,
        RecommendationContext.Midday => """
            CONTEXT: Midday check.
            - Select 1-2 candidates maximum
            - Focus on next best action for remaining day
            """,
        RecommendationContext.Onboarding => """
            CONTEXT: Onboarding.
            - Select 2-3 candidates maximum
            - Prioritize foundational elements
            - Use encouraging, approachable language
            """,
        RecommendationContext.ProactiveCheck => """
            CONTEXT: Proactive background check.
            - Select 3-5 candidates maximum
            - Prioritize highest-leverage interventions
            - Avoid duplicating pending recommendations
            """,
        _ => "Select 3-5 candidates based on the user's current state."
    };
}

/// <summary>
/// Stage 2 response model for candidate selection.
/// </summary>
internal sealed class CandidateSelectionResult
{
    [JsonPropertyName("selections")]
    public List<CandidateSelection> Selections { get; set; } = [];

    [JsonPropertyName("overallStrategy")]
    public string OverallStrategy { get; set; } = "";

    [JsonPropertyName("rejectedCandidatesReasoning")]
    public string? RejectedCandidatesReasoning { get; set; }
}

internal sealed class CandidateSelection
{
    [JsonPropertyName("candidateIndex")]
    public int CandidateIndex { get; set; }

    [JsonPropertyName("rationale")]
    public string Rationale { get; set; } = "";

    [JsonPropertyName("priorityRank")]
    public int PriorityRank { get; set; }

    [JsonPropertyName("refinedActionSummary")]
    public string? RefinedActionSummary { get; set; }
}

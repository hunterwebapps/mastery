using System.Text;
using Mastery.Application.Common.Models;
using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Enums;

namespace Mastery.Infrastructure.Services.Prompts;

/// <summary>
/// Stage 3 (Habit Domain): Generates habit-related recommendations.
/// Handles: HabitModeSuggestion, HabitFromLeadMetricSuggestion
/// </summary>
internal static class HabitGenerationPrompt
{
    public const string PromptVersion = "habit-gen-v2.0";
    public const string Model = "gpt-5-mini";
    public const string SchemaName = "habit_generation";

    public static readonly BinaryData ResponseSchema = BinaryData.FromString("""
        {
          "type": "object",
          "properties": {
            "recommendations": {
              "type": "array",
              "items": {
                "anyOf": [
                  {
                    "type": "object",
                    "properties": {
                      "type": { "type": "string", "enum": ["HabitModeSuggestion"] },
                      "targetKind": { "type": "string", "enum": ["Habit"] },
                      "targetEntityId": { "type": "string" },
                      "targetEntityTitle": { "type": "string" },
                      "actionKind": { "type": "string", "enum": ["Update"] },
                      "title": { "type": "string" },
                      "rationale": { "type": "string" },
                      "score": { "type": "number" },
                      "actionPayload": {
                        "type": "object",
                        "properties": {
                          "habitId": { "type": "string" },
                          "defaultMode": { "type": "string", "enum": ["Full", "Maintenance", "Minimum"] },
                          "_summary": { "type": "string" }
                        },
                        "required": ["habitId", "defaultMode", "_summary"],
                        "additionalProperties": false
                      }
                    },
                    "required": ["type", "targetKind", "targetEntityId", "targetEntityTitle", "actionKind", "title", "rationale", "score", "actionPayload"],
                    "additionalProperties": false
                  },
                  {
                    "type": "object",
                    "properties": {
                      "type": { "type": "string", "enum": ["HabitFromLeadMetricSuggestion"] },
                      "targetKind": { "type": "string", "enum": ["Habit"] },
                      "targetEntityId": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                      "targetEntityTitle": { "type": "string" },
                      "actionKind": { "type": "string", "enum": ["Create"] },
                      "title": { "type": "string" },
                      "rationale": { "type": "string" },
                      "score": { "type": "number" },
                      "actionPayload": {
                        "type": "object",
                        "properties": {
                          "title": { "type": "string" },
                          "description": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                          "why": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                          "defaultMode": { "type": "string", "enum": ["Full", "Maintenance", "Minimum"] },
                          "schedule": {
                            "type": "object",
                            "properties": {
                              "type": { "type": "string", "enum": ["Daily", "DaysOfWeek", "WeeklyFrequency", "Interval"] },
                              "daysOfWeek": { "anyOf": [{ "type": "array", "items": { "type": "integer", "minimum": 0, "maximum": 6 } }, { "type": "null" }] },
                              "frequencyPerWeek": { "anyOf": [{ "type": "integer", "minimum": 1, "maximum": 7 }, { "type": "null" }] },
                              "intervalDays": { "anyOf": [{ "type": "integer", "minimum": 1, "maximum": 90 }, { "type": "null" }] }
                            },
                            "required": ["type", "daysOfWeek", "frequencyPerWeek", "intervalDays"],
                            "additionalProperties": false
                          },
                          "goalIds": { "anyOf": [{ "type": "array", "items": { "type": "string" } }, { "type": "null" }] },
                          "variants": {
                            "anyOf": [
                              {
                                "type": "array",
                                "items": {
                                  "type": "object",
                                  "properties": {
                                    "mode": { "type": "string", "enum": ["Full", "Maintenance", "Minimum"] },
                                    "label": { "type": "string" },
                                    "estimatedMinutes": { "type": "integer" },
                                    "energyCost": { "type": "integer", "minimum": 1, "maximum": 5 }
                                  },
                                  "required": ["mode", "label", "estimatedMinutes", "energyCost"],
                                  "additionalProperties": false
                                }
                              },
                              { "type": "null" }
                            ]
                          },
                          "_summary": { "type": "string" }
                        },
                        "required": ["title", "description", "why", "defaultMode", "schedule", "goalIds", "variants", "_summary"],
                        "additionalProperties": false
                      }
                    },
                    "required": ["type", "targetKind", "targetEntityId", "targetEntityTitle", "actionKind", "title", "rationale", "score", "actionPayload"],
                    "additionalProperties": false
                  },
                  {
                    "type": "object",
                    "properties": {
                      "type": { "type": "string", "enum": ["HabitEditSuggestion"] },
                      "targetKind": { "type": "string", "enum": ["Habit"] },
                      "targetEntityId": { "type": "string" },
                      "targetEntityTitle": { "type": "string" },
                      "actionKind": { "type": "string", "enum": ["Update"] },
                      "title": { "type": "string" },
                      "rationale": { "type": "string" },
                      "score": { "type": "number" },
                      "actionPayload": {
                        "type": "object",
                        "properties": {
                          "habitId": { "type": "string" },
                          "newTitle": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                          "newDefaultMode": { "anyOf": [{ "type": "string", "enum": ["Full", "Maintenance", "Minimum"] }, { "type": "null" }] },
                          "newSchedule": {
                            "anyOf": [
                              {
                                "type": "object",
                                "properties": {
                                  "type": { "type": "string", "enum": ["Daily", "DaysOfWeek", "WeeklyFrequency", "Interval"] },
                                  "daysOfWeek": { "anyOf": [{ "type": "array", "items": { "type": "integer", "minimum": 0, "maximum": 6 } }, { "type": "null" }] },
                                  "frequencyPerWeek": { "anyOf": [{ "type": "integer", "minimum": 1, "maximum": 7 }, { "type": "null" }] },
                                  "intervalDays": { "anyOf": [{ "type": "integer", "minimum": 1, "maximum": 90 }, { "type": "null" }] }
                                },
                                "required": ["type", "daysOfWeek", "frequencyPerWeek", "intervalDays"],
                                "additionalProperties": false
                              },
                              { "type": "null" }
                            ]
                          },
                          "_summary": { "type": "string" }
                        },
                        "required": ["habitId", "newTitle", "newDefaultMode", "newSchedule", "_summary"],
                        "additionalProperties": false
                      }
                    },
                    "required": ["type", "targetKind", "targetEntityId", "targetEntityTitle", "actionKind", "title", "rationale", "score", "actionPayload"],
                    "additionalProperties": false
                  },
                  {
                    "type": "object",
                    "properties": {
                      "type": { "type": "string", "enum": ["HabitArchiveSuggestion"] },
                      "targetKind": { "type": "string", "enum": ["Habit"] },
                      "targetEntityId": { "type": "string" },
                      "targetEntityTitle": { "type": "string" },
                      "actionKind": { "type": "string", "enum": ["Remove"] },
                      "title": { "type": "string" },
                      "rationale": { "type": "string" },
                      "score": { "type": "number" },
                      "actionPayload": {
                        "type": "object",
                        "properties": {
                          "habitId": { "type": "string" },
                          "_summary": { "type": "string" }
                        },
                        "required": ["habitId", "_summary"],
                        "additionalProperties": false
                      }
                    },
                    "required": ["type", "targetKind", "targetEntityId", "targetEntityTitle", "actionKind", "title", "rationale", "score", "actionPayload"],
                    "additionalProperties": false
                  }
                ]
              }
            }
          },
          "required": ["recommendations"],
          "additionalProperties": false
        }
        """);

    public static string BuildSystemPrompt()
    {
        return """
            You are generating habit-related recommendations for the Mastery personal development system.
            You will receive a situational assessment, intervention plan items, and relevant habit/metric data.

            For each intervention plan item assigned to you, generate ONE recommendation with a complete actionPayload.

            ## Recommendation Types You Handle

            ### HabitModeSuggestion (ActionKind: Update)
            Suggest scaling an existing habit to minimum mode when energy is low or adherence is dropping.
            actionPayload: { "habitId": "guid-string", "defaultMode": "Minimum", "_summary": "Switch 'Morning workout' to minimum mode" }

            ### HabitFromLeadMetricSuggestion (ActionKind: Create)
            Create a new habit to drive an orphaned lead metric (a metric with no habit bound to it).
            actionPayload: {
              "title": "string",
              "description": "string or null",
              "why": "string or null (connects habit to the metric/goal)",
              "defaultMode": "Full" | "Maintenance" | "Minimum",
              "schedule": {
                "type": "Daily" | "DaysOfWeek" | "WeeklyFrequency" | "Interval",
                "daysOfWeek": [0-6] or null (0=Sunday, for DaysOfWeek type),
                "frequencyPerWeek": number or null (1-7, for WeeklyFrequency type),
                "intervalDays": number or null (1-90, for Interval type)
              },
              "goalIds": ["guid-string"] or null (link to goals this habit drives),
              "variants": [
                { "mode": "Full", "label": "30 min workout", "estimatedMinutes": 30, "energyCost": 4 },
                { "mode": "Minimum", "label": "5 min stretch", "estimatedMinutes": 5, "energyCost": 1 }
              ] or null,
              "_summary": "Create daily habit 'Read 10 pages'"
            }

            ### HabitEditSuggestion (ActionKind: Update)
            Suggest modifying an existing habit's title, mode, or schedule.
            actionPayload: {
              "habitId": "guid-string (required)",
              "newTitle": "string (optional)",
              "newDefaultMode": "Full" | "Maintenance" | "Minimum" (optional),
              "newSchedule": { schedule object } (optional),
              "_summary": "Change 'Workout' schedule to Mon/Wed/Fri"
            }

            ### HabitArchiveSuggestion (ActionKind: Remove)
            Suggest archiving a habit that is no longer relevant or has been superseded.
            actionPayload: {
              "habitId": "guid-string (required)",
              "_summary": "Archive habit (0% adherence for 3 weeks)"
            }

            ## Field Reference

            """ + SchemaReference.HabitModeSchema + """

            """ + SchemaReference.ScheduleTypeSchema + """

            """ + SchemaReference.HabitVariantSchema + """

            """ + SchemaReference.EnergyCostSchema + """

            """ + SchemaReference.HabitFieldGuidance + """

            ## Guidelines
            - Score MUST be 0.0-1.0 where 0.0=minimal impact, 1.0=maximum urgency/impact (e.g., 0.85 for high priority)
            - For mode suggestions: frame it as protecting the streak, not giving up
            - For new habits: start small (suggest minimum mode or simple schedule)
            - Connect the habit to the user's goals in the rationale
            - A habit driving a lead metric is a high-leverage intervention — score accordingly
            - Never suggest creating a habit that already exists
            - For Edit: only include fields that need to change
            - For Archive: use when habit has very low adherence for extended period or is no longer aligned with goals
            - ALWAYS include _summary in actionPayload - this is shown to the user before they accept
            - _summary should be concise (under 60 chars) and describe exactly what will happen

            ## Historical Context Usage
            If related habit history is provided:
            - Use successful mode switches as templates (e.g., if switching to Minimum worked before, suggest it again)
            - Avoid habit suggestions similar to recently dismissed ones
            - Reference past adherence patterns when suggesting schedules
            - Build on habits that were successfully created and maintained
            """;
    }

    public static string BuildUserPrompt(
        SituationalAssessment assessment,
        IReadOnlyList<InterventionPlanItem> habitInterventions,
        IReadOnlyList<HabitSnapshot> habits,
        IReadOnlyList<GoalSnapshot> goals,
        IReadOnlyList<UserValueSnapshot>? values = null,
        IReadOnlyList<UserRoleSnapshot>? roles = null,
        IReadOnlyList<MetricDefinitionSnapshot>? metrics = null,
        DateOnly? today = null,
        RagContext? ragContext = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Assessment: {assessment.CapacityStatus} capacity, {assessment.EnergyTrend} energy");
        if (today.HasValue)
            sb.AppendLine($"Today: {today.Value:yyyy-MM-dd} ({today.Value.DayOfWeek})");
        sb.AppendLine();

        // User values for habit alignment
        if (values is { Count: > 0 })
        {
            sb.AppendLine("# User's Core Values (for habit alignment)");
            foreach (var v in values.Take(5))
                sb.AppendLine($"  - {v.Label}");
            sb.AppendLine();
        }

        // Active roles for context tagging
        var activeRoles = roles?.Where(r => r.IsActive).ToList() ?? [];
        if (activeRoles.Count > 0)
        {
            sb.AppendLine("# Active Roles (habits can serve these)");
            foreach (var r in activeRoles.OrderByDescending(r => r.SeasonPriority).Take(5))
                sb.AppendLine($"  - {r.Label} | SeasonPriority:{r.SeasonPriority}/5 | Tags:[{string.Join(", ", r.Tags)}]");
            sb.AppendLine();
        }

        sb.AppendLine("# Intervention Plan (your assignments):");
        foreach (var item in habitInterventions)
        {
            sb.AppendLine($"- [{item.Priority}] {item.Area}: {item.Reasoning}");
            sb.AppendLine($"  Target type: {item.TargetType}");
            if (item.TargetEntityIds is { Count: > 0 })
                sb.AppendLine($"  Target entities: {string.Join(", ", item.TargetEntityIds)}");
        }
        sb.AppendLine();

        var active = habits.Where(h => h.Status == HabitStatus.Active).ToList();
        sb.AppendLine($"# Current Habits ({active.Count} active)");
        foreach (var h in active)
        {
            var scheduleStr = FormatSchedule(h.Schedule);
            var variantStr = h.Variants?.Count > 0
                ? $" | Variants:[{string.Join(", ", h.Variants.Select(v => $"{v.Mode}:{v.Label}"))}]"
                : "";
            var goalsStr = h.GoalIds?.Count > 0 ? " | GoalBound:yes" : "";
            sb.AppendLine($"- [{h.Id}] \"{h.Title}\" | Mode:{h.CurrentMode} | {scheduleStr} | Adherence7d:{h.Adherence7Day:P0} | Streak:{h.CurrentStreak} | MetricBound:{(h.MetricBindingIds.Count > 0 ? "yes" : "no")}{goalsStr}{variantStr}");
        }
        sb.AppendLine();

        // Active/Draft Goals with lead metrics
        var activeGoals = goals.Where(g => g.Status == GoalStatus.Active || g.Status == GoalStatus.Draft).ToList();
        if (activeGoals.Count > 0)
        {
            sb.AppendLine("# Goals (for linking via goalIds)");
            foreach (var g in activeGoals)
            {
                sb.AppendLine($"- [{g.Id}] \"{g.Title}\" | Status:{g.Status} | P{g.Priority}");
                foreach (var m in g.Metrics.Where(m => m.Kind == MetricKind.Lead))
                    sb.AppendLine($"    Lead: \"{m.MetricName}\" | Target:{m.TargetValue} | Current:{m.CurrentValue?.ToString() ?? "?"} | Source:{m.SourceHint}");
            }
            sb.AppendLine();
        }

        // Available metrics for binding
        var habitMetrics = metrics?.Where(m => m.SourceType == MetricSourceType.Habit).Take(10).ToList();
        if (habitMetrics is { Count: > 0 })
        {
            sb.AppendLine("# Available Metrics (habit-sourced, for metric bindings)");
            foreach (var m in habitMetrics)
                sb.AppendLine($"  [{m.Id}] \"{m.Name}\" ({m.DataType})");
            sb.AppendLine();
        }

        // Add critical constraints to prevent hallucinated IDs
        sb.AppendLine("## CRITICAL CONSTRAINTS");
        if (active.Count == 0)
        {
            sb.AppendLine("- NO HABITS EXIST. Only generate HabitFromLeadMetricSuggestion (Create) with targetEntityId: null.");
            sb.AppendLine("- Do NOT generate HabitModeSuggestion, HabitEditSuggestion, or HabitArchiveSuggestion.");
        }
        else
        {
            sb.AppendLine("- For Update/Remove actions, you MUST use a habitId from the list above.");
            sb.AppendLine($"- VALID HABIT IDS: {string.Join(", ", active.Select(h => h.Id))}");
            sb.AppendLine("- Do NOT invent or hallucinate habit IDs. Only use IDs that appear in the Current Habits list.");
        }

        if (activeGoals.Count > 0)
            sb.AppendLine($"- VALID GOAL IDS (for goalIds): {string.Join(", ", activeGoals.Select(g => g.Id))}");
        else
            sb.AppendLine("- NO GOALS EXIST. Set goalIds to null in Create payloads.");

        sb.AppendLine();

        // Add RAG historical context BEFORE generating - inform recommendations
        RagContextFormatter.AppendForGeneration(sb, ragContext, "Habit", today ?? DateOnly.FromDateTime(DateTime.UtcNow));

        sb.AppendLine("Generate recommendations for each intervention plan item assigned to you.");
        return sb.ToString();
    }

    private static string FormatSchedule(HabitScheduleSnapshot? schedule)
    {
        if (schedule is null) return "Schedule:unknown";

        return schedule.Type switch
        {
            "Daily" => "Daily",
            "DaysOfWeek" when schedule.DaysOfWeek is not null =>
                $"Days:[{string.Join(",", schedule.DaysOfWeek.Select(d => ((DayOfWeek)d).ToString()[..3]))}]",
            "WeeklyFrequency" when schedule.FrequencyPerWeek.HasValue =>
                $"{schedule.FrequencyPerWeek}x/week",
            "Interval" when schedule.IntervalDays.HasValue =>
                $"Every{schedule.IntervalDays}days",
            _ => $"Schedule:{schedule.Type}"
        };
    }
}

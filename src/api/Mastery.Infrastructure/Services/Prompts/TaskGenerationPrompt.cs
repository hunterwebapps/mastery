using System.Text;
using System.Text.Json.Serialization;
using Mastery.Application.Common.Models;
using Mastery.Domain.Enums;

namespace Mastery.Infrastructure.Services.Prompts;

/// <summary>
/// Stage 3 (Task Domain): Generates task-related recommendations.
/// Handles: NextBestAction, TaskBreakdownSuggestion, ScheduleAdjustmentSuggestion, PlanRealismAdjustment
/// </summary>
internal static class TaskGenerationPrompt
{
    public const string PromptVersion = "task-gen-v2.0";
    public const string Model = "gpt-5-mini";
    public const string SchemaName = "task_generation";

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
                      "type": { "type": "string", "enum": ["NextBestAction"] },
                      "targetKind": { "type": "string", "enum": ["Task"] },
                      "targetEntityId": { "type": "string" },
                      "targetEntityTitle": { "type": "string" },
                      "actionKind": { "type": "string", "enum": ["ExecuteToday"] },
                      "title": { "type": "string" },
                      "rationale": { "type": "string" },
                      "score": { "type": "number" },
                      "actionPayload": {
                        "type": "object",
                        "properties": {
                          "taskId": { "type": "string" },
                          "_summary": { "type": "string" }
                        },
                        "required": ["taskId", "_summary"],
                        "additionalProperties": false
                      }
                    },
                    "required": ["type", "targetKind", "targetEntityId", "targetEntityTitle", "actionKind", "title", "rationale", "score", "actionPayload"],
                    "additionalProperties": false
                  },
                  {
                    "type": "object",
                    "properties": {
                      "type": { "type": "string", "enum": ["TaskBreakdownSuggestion"] },
                      "targetKind": { "type": "string", "enum": ["Task"] },
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
                          "estMinutes": { "type": "integer" },
                          "energyCost": { "type": "integer" },
                          "priority": { "type": "integer" },
                          "projectId": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                          "goalId": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                          "contextTags": { "anyOf": [{ "type": "array", "items": { "type": "string" } }, { "type": "null" }] },
                          "dueOn": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                          "dueType": { "anyOf": [{ "type": "string", "enum": ["Soft", "Hard"] }, { "type": "null" }] },
                          "startAsReady": { "type": "boolean" },
                          "_summary": { "type": "string" }
                        },
                        "required": ["title", "description", "estMinutes", "energyCost", "priority", "projectId", "goalId", "contextTags", "dueOn", "dueType", "startAsReady", "_summary"],
                        "additionalProperties": false
                      }
                    },
                    "required": ["type", "targetKind", "targetEntityId", "targetEntityTitle", "actionKind", "title", "rationale", "score", "actionPayload"],
                    "additionalProperties": false
                  },
                  {
                    "type": "object",
                    "properties": {
                      "type": { "type": "string", "enum": ["ScheduleAdjustmentSuggestion"] },
                      "targetKind": { "type": "string", "enum": ["Task"] },
                      "targetEntityId": { "type": "string" },
                      "targetEntityTitle": { "type": "string" },
                      "actionKind": { "type": "string", "enum": ["Defer"] },
                      "title": { "type": "string" },
                      "rationale": { "type": "string" },
                      "score": { "type": "number" },
                      "actionPayload": {
                        "type": "object",
                        "properties": {
                          "taskId": { "type": "string" },
                          "newDate": { "type": "string" },
                          "reason": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                          "_summary": { "type": "string" }
                        },
                        "required": ["taskId", "newDate", "reason", "_summary"],
                        "additionalProperties": false
                      }
                    },
                    "required": ["type", "targetKind", "targetEntityId", "targetEntityTitle", "actionKind", "title", "rationale", "score", "actionPayload"],
                    "additionalProperties": false
                  },
                  {
                    "type": "object",
                    "properties": {
                      "type": { "type": "string", "enum": ["PlanRealismAdjustment"] },
                      "targetKind": { "type": "string", "enum": ["Task"] },
                      "targetEntityId": { "type": "string" },
                      "targetEntityTitle": { "type": "string" },
                      "actionKind": { "type": "string", "enum": ["Defer"] },
                      "title": { "type": "string" },
                      "rationale": { "type": "string" },
                      "score": { "type": "number" },
                      "actionPayload": {
                        "type": "object",
                        "properties": {
                          "taskId": { "type": "string" },
                          "newDate": { "type": "string" },
                          "reason": { "type": "string" },
                          "_summary": { "type": "string" }
                        },
                        "required": ["taskId", "newDate", "reason", "_summary"],
                        "additionalProperties": false
                      }
                    },
                    "required": ["type", "targetKind", "targetEntityId", "targetEntityTitle", "actionKind", "title", "rationale", "score", "actionPayload"],
                    "additionalProperties": false
                  },
                  {
                    "type": "object",
                    "properties": {
                      "type": { "type": "string", "enum": ["TaskEditSuggestion"] },
                      "targetKind": { "type": "string", "enum": ["Task"] },
                      "targetEntityId": { "type": "string" },
                      "targetEntityTitle": { "type": "string" },
                      "actionKind": { "type": "string", "enum": ["Update"] },
                      "title": { "type": "string" },
                      "rationale": { "type": "string" },
                      "score": { "type": "number" },
                      "actionPayload": {
                        "type": "object",
                        "properties": {
                          "taskId": { "type": "string" },
                          "newTitle": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                          "newDescription": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                          "newPriority": { "anyOf": [{ "type": "integer" }, { "type": "null" }] },
                          "newEstMinutes": { "anyOf": [{ "type": "integer" }, { "type": "null" }] },
                          "newEnergyCost": { "anyOf": [{ "type": "integer" }, { "type": "null" }] },
                          "_summary": { "type": "string" }
                        },
                        "required": ["taskId", "newTitle", "newDescription", "newPriority", "newEstMinutes", "newEnergyCost", "_summary"],
                        "additionalProperties": false
                      }
                    },
                    "required": ["type", "targetKind", "targetEntityId", "targetEntityTitle", "actionKind", "title", "rationale", "score", "actionPayload"],
                    "additionalProperties": false
                  },
                  {
                    "type": "object",
                    "properties": {
                      "type": { "type": "string", "enum": ["TaskArchiveSuggestion"] },
                      "targetKind": { "type": "string", "enum": ["Task"] },
                      "targetEntityId": { "type": "string" },
                      "targetEntityTitle": { "type": "string" },
                      "actionKind": { "type": "string", "enum": ["Remove"] },
                      "title": { "type": "string" },
                      "rationale": { "type": "string" },
                      "score": { "type": "number" },
                      "actionPayload": {
                        "type": "object",
                        "properties": {
                          "taskId": { "type": "string" },
                          "_summary": { "type": "string" }
                        },
                        "required": ["taskId", "_summary"],
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
            You are generating task-related recommendations for the Mastery personal development system.
            You will receive a situational assessment, intervention plan items, and relevant task data.

            For each intervention plan item assigned to you, generate ONE recommendation with a complete actionPayload.

            ## Recommendation Types You Handle

            ### NextBestAction (ActionKind: ExecuteToday)
            Suggest scheduling an existing task for today.
            actionPayload: { "taskId": "guid-string", "_summary": "Schedule 'Design API schema' for today" }

            ### TaskBreakdownSuggestion (ActionKind: Create)
            Create a new subtask to break down a large or stuck task.
            actionPayload: {
              "title": "string",
              "description": "string or null",
              "estMinutes": number (10-120),
              "energyCost": number (1-5, where 1=low 5=high),
              "priority": number (1-5, where 1=highest)",
              "projectId": "guid-string or null (link to parent project)",
              "goalId": "guid-string or null (link to goal)",
              "contextTags": ["string"] or null,
              "dueOn": "YYYY-MM-DD or null",
              "dueType": "Soft" | "Hard" or null,
              "startAsReady": true,
              "_summary": "Create task 'Design API schema' (30 min, priority 3)"
            }

            ### ScheduleAdjustmentSuggestion (ActionKind: Defer)
            Move a task to a better day.
            actionPayload: {
              "taskId": "guid-string",
              "newDate": "YYYY-MM-DD",
              "reason": "string or null",
              "_summary": "Move 'Write tests' to Thursday (Jan 30)"
            }

            ### PlanRealismAdjustment (ActionKind: Defer)
            Move a task off an overloaded day.
            actionPayload: {
              "taskId": "guid-string",
              "newDate": "YYYY-MM-DD",
              "reason": "string",
              "_summary": "Defer 'Review PRs' to Friday to reduce overload"
            }

            ### TaskEditSuggestion (ActionKind: Update)
            Suggest modifying an existing task (title, priority, time estimate, etc.).
            actionPayload: {
              "taskId": "guid-string (required)",
              "newTitle": "string (optional)",
              "newDescription": "string (optional)",
              "newPriority": integer (optional, 1-5),
              "newEstMinutes": integer (optional)",
              "newEnergyCost": integer (optional, 1-5),
              "_summary": "Update priority from 5 to 2"
            }

            ### TaskArchiveSuggestion (ActionKind: Remove)
            Suggest archiving a stale or irrelevant task.
            actionPayload: {
              "taskId": "guid-string (required)",
              "_summary": "Archive stale task (no activity for 14 days)"
            }

            ## Field Reference

            """ + SchemaReference.ContextTagSchema + """

            """ + SchemaReference.PrioritySchema + """

            """ + SchemaReference.EnergyCostSchema + """

            """ + SchemaReference.DueTypeSchema + """

            """ + SchemaReference.TaskFieldGuidance + """

            ## Guidelines
            - Title should be imperative and concise: "Complete: Design API schema" or "Move 'Write tests' to Thursday"
            - Rationale should be personal and specific, referencing the user's actual situation
            - Score reflects urgency and impact (higher = more important)
            - For ExecuteToday: the task MUST already exist (use its ID)
            - For Create: the task is new (targetEntityId is null)
            - For Defer: the task MUST already exist (use its ID), and newDate must be in the future
            - For Update: only include fields that need to change
            - For Remove: use when a task is stale (>14 days no activity) or no longer relevant
            - ALWAYS include _summary in actionPayload - this is shown to the user before they accept
            - _summary should be concise (under 60 chars) and describe exactly what will happen
            """;
    }

    public static string BuildUserPrompt(
        SituationalAssessment assessment,
        IReadOnlyList<InterventionPlanItem> taskInterventions,
        IReadOnlyList<TaskSnapshot> tasks,
        DateOnly today,
        ConstraintsSnapshot? constraints = null,
        IReadOnlyList<ProjectSnapshot>? projects = null,
        IReadOnlyList<GoalSnapshot>? goals = null,
        IReadOnlyList<UserRoleSnapshot>? roles = null,
        IReadOnlyList<UserValueSnapshot>? values = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Assessment Summary: {assessment.CapacityStatus} capacity, {assessment.EnergyTrend} energy, {assessment.OverallMomentum} momentum");
        sb.AppendLine($"Today: {today:yyyy-MM-dd} ({today.DayOfWeek})");

        // Add capacity constraints
        if (constraints is not null)
        {
            var isWeekend = today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var maxMinutes = isWeekend ? constraints.MaxPlannedMinutesWeekend : constraints.MaxPlannedMinutesWeekday;
            sb.AppendLine($"Daily capacity limit: {maxMinutes}min (user-defined {(isWeekend ? "weekend" : "weekday")} max)");
        }
        sb.AppendLine();

        sb.AppendLine("# Intervention Plan (your assignments):");
        foreach (var item in taskInterventions)
        {
            sb.AppendLine($"- [{item.Priority}] {item.Area}: {item.Reasoning}");
            sb.AppendLine($"  Target type: {item.TargetType}");
            if (item.TargetEntityIds is { Count: > 0 })
                sb.AppendLine($"  Target entities: {string.Join(", ", item.TargetEntityIds)}");
        }
        sb.AppendLine();

        var actionable = tasks
            .Where(t => t.Status != Domain.Enums.TaskStatus.Completed &&
                        t.Status != Domain.Enums.TaskStatus.Cancelled &&
                        t.Status != Domain.Enums.TaskStatus.Archived)
            .OrderBy(t => t.Priority)
            .Take(15)
            .ToList();

        sb.AppendLine($"# Available Tasks ({actionable.Count})");
        foreach (var t in actionable)
        {
            var scheduled = t.ScheduledDate == today ? "TODAY" : t.ScheduledDate?.ToString("MMM dd") ?? "unscheduled";
            var due = t.DueDate is not null ? $"Due:{t.DueDate:MMM dd}" : "";
            var tags = t.ContextTags.Count > 0 ? $"Tags:[{string.Join(",", t.ContextTags)}]" : "";
            sb.AppendLine($"- [{t.Id}] \"{t.Title}\" | {t.Status} | P{t.Priority} | Energy:{t.EnergyLevel} | Est:{t.EstMinutes ?? 0}min | {scheduled} {due} {tags} | Reschedules:{t.RescheduleCount}");
        }
        sb.AppendLine();

        // Available Projects for linking
        var activeProjects = projects?.Where(p => p.Status == ProjectStatus.Active).Take(10).ToList();
        if (activeProjects is { Count: > 0 })
        {
            sb.AppendLine("# Available Projects (for projectId)");
            foreach (var p in activeProjects)
                sb.AppendLine($"  [{p.Id}] \"{p.Title}\"");
            sb.AppendLine();
        }

        // Available Goals for linking
        var activeGoals = goals?.Where(g => g.Status == GoalStatus.Active).Take(8).ToList();
        if (activeGoals is { Count: > 0 })
        {
            sb.AppendLine("# Available Goals (for goalId)");
            foreach (var g in activeGoals)
                sb.AppendLine($"  [{g.Id}] \"{g.Title}\" | P{g.Priority}");
            sb.AppendLine();
        }

        // User Roles for alignment
        var activeRoles = roles?.Where(r => r.IsActive).OrderByDescending(r => r.SeasonPriority).Take(5).ToList();
        if (activeRoles is { Count: > 0 })
        {
            sb.AppendLine("# User Roles (for context)");
            foreach (var r in activeRoles)
                sb.AppendLine($"  [{r.Id}] \"{r.Label}\" | SeasonPriority:{r.SeasonPriority}");
            sb.AppendLine();
        }

        // User Values for alignment
        if (values is { Count: > 0 })
        {
            sb.AppendLine("# User Values (for context)");
            foreach (var v in values.Take(5))
                sb.AppendLine($"  - \"{v.Label}\"");
            sb.AppendLine();
        }

        // Add critical constraints to prevent hallucinated IDs
        sb.AppendLine("## CRITICAL CONSTRAINTS");
        if (actionable.Count == 0)
        {
            sb.AppendLine("- NO TASKS EXIST. Only generate TaskBreakdownSuggestion (Create) with targetEntityId: null.");
            sb.AppendLine("- Do NOT generate NextBestAction, ScheduleAdjustmentSuggestion, PlanRealismAdjustment, TaskEditSuggestion, or TaskArchiveSuggestion.");
        }
        else
        {
            sb.AppendLine("- For Update/Remove/ExecuteToday/Defer actions, you MUST use a taskId from the list above.");
            sb.AppendLine($"- VALID TASK IDS: {string.Join(", ", actionable.Select(t => t.Id))}");
            sb.AppendLine("- Do NOT invent or hallucinate task IDs. Only use IDs that appear in the Available Tasks list.");
        }

        if (activeProjects is { Count: > 0 })
            sb.AppendLine($"- VALID PROJECT IDS (for projectId): {string.Join(", ", activeProjects.Select(p => p.Id))}");
        else
            sb.AppendLine("- NO PROJECTS EXIST. Set projectId to null in Create payloads.");

        if (activeGoals is { Count: > 0 })
            sb.AppendLine($"- VALID GOAL IDS (for goalId): {string.Join(", ", activeGoals.Select(g => g.Id))}");
        else
            sb.AppendLine("- NO GOALS EXIST. Set goalId to null in Create payloads.");

        sb.AppendLine();
        sb.AppendLine("Generate recommendations for each intervention plan item assigned to you.");

        return sb.ToString();
    }
}

using System.Text;
using Mastery.Application.Common.Models;
using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Enums;
using TaskStatus = Mastery.Domain.Enums.TaskStatus;

namespace Mastery.Infrastructure.Services.Prompts;

/// <summary>
/// Stage 3 (Project Domain): Generates project-related recommendations.
/// Handles: ProjectStuckFix, ProjectSuggestion, ProjectEditSuggestion, ProjectArchiveSuggestion, ProjectGoalLinkSuggestion
/// </summary>
internal static class ProjectGenerationPrompt
{
    public const string PromptVersion = "project-gen-v1.0";
    public const string Model = "gpt-5-mini";
    public const string SchemaName = "project_generation";

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
                      "type": { "type": "string", "enum": ["ProjectStuckFix"] },
                      "targetKind": { "type": "string", "enum": ["Project"] },
                      "targetEntityId": { "type": "string" },
                      "targetEntityTitle": { "type": "string" },
                      "actionKind": { "type": "string", "enum": ["Update"] },
                      "title": { "type": "string" },
                      "rationale": { "type": "string" },
                      "score": { "type": "number" },
                      "actionPayload": {
                        "type": "object",
                        "properties": {
                          "projectId": { "type": "string" },
                          "suggestedNextTaskId": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                          "suggestedNewTask": {
                            "anyOf": [
                              {
                                "type": "object",
                                "properties": {
                                  "title": { "type": "string" },
                                  "description": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                                  "estMinutes": { "type": "integer" },
                                  "energyCost": { "type": "integer" },
                                  "priority": { "type": "integer" }
                                },
                                "required": ["title", "description", "estMinutes", "energyCost", "priority"],
                                "additionalProperties": false
                              },
                              { "type": "null" }
                            ]
                          },
                          "_summary": { "type": "string" }
                        },
                        "required": ["projectId", "suggestedNextTaskId", "suggestedNewTask", "_summary"],
                        "additionalProperties": false
                      }
                    },
                    "required": ["type", "targetKind", "targetEntityId", "targetEntityTitle", "actionKind", "title", "rationale", "score", "actionPayload"],
                    "additionalProperties": false
                  },
                  {
                    "type": "object",
                    "properties": {
                      "type": { "type": "string", "enum": ["ProjectSuggestion"] },
                      "targetKind": { "type": "string", "enum": ["Project"] },
                      "targetEntityId": { "type": "null" },
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
                          "priority": { "type": "integer" },
                          "goalId": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                          "targetEndDate": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                          "milestones": {
                            "anyOf": [
                              {
                                "type": "array",
                                "items": {
                                  "type": "object",
                                  "properties": {
                                    "title": { "type": "string" },
                                    "targetDate": { "anyOf": [{ "type": "string" }, { "type": "null" }] }
                                  },
                                  "required": ["title", "targetDate"],
                                  "additionalProperties": false
                                }
                              },
                              { "type": "null" }
                            ]
                          },
                          "_summary": { "type": "string" }
                        },
                        "required": ["title", "description", "priority", "goalId", "targetEndDate", "milestones", "_summary"],
                        "additionalProperties": false
                      }
                    },
                    "required": ["type", "targetKind", "targetEntityId", "targetEntityTitle", "actionKind", "title", "rationale", "score", "actionPayload"],
                    "additionalProperties": false
                  },
                  {
                    "type": "object",
                    "properties": {
                      "type": { "type": "string", "enum": ["ProjectEditSuggestion"] },
                      "targetKind": { "type": "string", "enum": ["Project"] },
                      "targetEntityId": { "type": "string" },
                      "targetEntityTitle": { "type": "string" },
                      "actionKind": { "type": "string", "enum": ["Update"] },
                      "title": { "type": "string" },
                      "rationale": { "type": "string" },
                      "score": { "type": "number" },
                      "actionPayload": {
                        "type": "object",
                        "properties": {
                          "projectId": { "type": "string" },
                          "newTitle": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                          "newDescription": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                          "newPriority": { "anyOf": [{ "type": "integer" }, { "type": "null" }] },
                          "newGoalId": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                          "newTargetEndDate": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                          "_summary": { "type": "string" }
                        },
                        "required": ["projectId", "newTitle", "newDescription", "newPriority", "newGoalId", "newTargetEndDate", "_summary"],
                        "additionalProperties": false
                      }
                    },
                    "required": ["type", "targetKind", "targetEntityId", "targetEntityTitle", "actionKind", "title", "rationale", "score", "actionPayload"],
                    "additionalProperties": false
                  },
                  {
                    "type": "object",
                    "properties": {
                      "type": { "type": "string", "enum": ["ProjectArchiveSuggestion"] },
                      "targetKind": { "type": "string", "enum": ["Project"] },
                      "targetEntityId": { "type": "string" },
                      "targetEntityTitle": { "type": "string" },
                      "actionKind": { "type": "string", "enum": ["Remove"] },
                      "title": { "type": "string" },
                      "rationale": { "type": "string" },
                      "score": { "type": "number" },
                      "actionPayload": {
                        "type": "object",
                        "properties": {
                          "projectId": { "type": "string" },
                          "_summary": { "type": "string" }
                        },
                        "required": ["projectId", "_summary"],
                        "additionalProperties": false
                      }
                    },
                    "required": ["type", "targetKind", "targetEntityId", "targetEntityTitle", "actionKind", "title", "rationale", "score", "actionPayload"],
                    "additionalProperties": false
                  },
                  {
                    "type": "object",
                    "properties": {
                      "type": { "type": "string", "enum": ["ProjectGoalLinkSuggestion"] },
                      "targetKind": { "type": "string", "enum": ["Project"] },
                      "targetEntityId": { "type": "string" },
                      "targetEntityTitle": { "type": "string" },
                      "actionKind": { "type": "string", "enum": ["Update"] },
                      "title": { "type": "string" },
                      "rationale": { "type": "string" },
                      "score": { "type": "number" },
                      "actionPayload": {
                        "type": "object",
                        "properties": {
                          "projectId": { "type": "string" },
                          "goalId": { "type": "string" },
                          "goalTitle": { "type": "string" },
                          "_summary": { "type": "string" }
                        },
                        "required": ["projectId", "goalId", "goalTitle", "_summary"],
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
            You are generating project-related recommendations for the Mastery personal development system.
            You will receive a situational assessment, intervention plan items, and project/task data.

            For each intervention plan item assigned to you, generate ONE recommendation with a complete actionPayload.

            ## Recommendation Types You Handle

            ### ProjectStuckFix (ActionKind: Update)
            Unblock a stuck project (active but no next action) by either:
            - Suggesting an existing task as the next action (PREFERRED), OR
            - Suggesting a new task to create and set as next action (only if no suitable existing task)

            IMPORTANT: Always check if the project already has tasks that could be the next action.
            Use suggestedNextTaskId when an existing task fits. Only use suggestedNewTask when:
            - No tasks exist for the project, OR
            - All existing tasks are too large (>120 min), OR
            - Existing tasks are blocked or not appropriate for the current phase

            actionPayload: {
              "projectId": "guid-string",
              "suggestedNextTaskId": "guid-string (PREFERRED: use existing task)" OR null,
              "suggestedNewTask": {
                "title": "string",
                "description": "string or null",
                "estMinutes": number (10-120),
                "energyCost": number (1-5),
                "priority": number (1-5)
              } OR null (only if no suitable existing task),
              "_summary": "Set 'Review architecture doc' as next action"
            }

            ### ProjectSuggestion (ActionKind: Create)
            Suggest creating a new project to organize work toward a goal.
            actionPayload: {
              "title": "string",
              "description": "string or null",
              "priority": number (1-5, where 1=highest)",
              "goalId": "guid-string or null (link to goal)",
              "targetEndDate": "YYYY-MM-DD or null",
              "milestones": [
                { "title": "string", "targetDate": "YYYY-MM-DD or null" }
              ] or null,
              "_summary": "Create project 'Launch MVP' linked to Q1 goal"
            }

            ### ProjectEditSuggestion (ActionKind: Update)
            Suggest modifying an existing project (title, priority, goal link, etc.).
            actionPayload: {
              "projectId": "guid-string (required)",
              "newTitle": "string (optional)",
              "newDescription": "string (optional)",
              "newPriority": integer (optional, 1-5)",
              "newGoalId": "guid-string (optional)",
              "newTargetEndDate": "YYYY-MM-DD (optional)",
              "_summary": "Update priority from 3 to 1"
            }

            ### ProjectArchiveSuggestion (ActionKind: Remove)
            Suggest archiving a stale or completed project.
            actionPayload: {
              "projectId": "guid-string (required)",
              "_summary": "Archive completed project"
            }

            ### ProjectGoalLinkSuggestion (ActionKind: Update)
            Suggest linking an unattached project to a relevant goal for better tracking.
            Use when: Project has no GoalId but clearly relates to an active goal by theme, title, or deadline alignment.
            actionPayload: {
              "projectId": "guid-string (the unlinked project)",
              "goalId": "guid-string (the goal to link to)",
              "goalTitle": "string (for display, e.g., 'Acquire an Additional Client')",
              "_summary": "Link to 'Acquire an Additional Client' goal"
            }

            ## Field Reference

            """ + SchemaReference.PrioritySchema + """

            """ + SchemaReference.MilestoneSchema + """

            """ + SchemaReference.ProjectFieldGuidance + """

            ## Guidelines
            - Score MUST be 0.0-1.0 where 0.0=minimal impact, 1.0=maximum urgency/impact (e.g., 0.85 for high priority)
            - Projects are execution containers that group tasks toward a goal
            - ProjectStuckFix is high priority: active projects without a next action block progress
            - When fixing stuck projects, prefer suggesting existing unscheduled tasks over creating new ones
            - New projects should be linked to goals when possible
            - Archive projects that have been inactive for >30 days or are no longer aligned with current season
            - For Edit: only include fields that need to change
            - ALWAYS include _summary in actionPayload - this is shown to the user before they accept
            - _summary should be concise (under 60 chars) and describe exactly what will happen

            ## Historical Context Usage
            If related project history is provided:
            - Use successful stuck project fixes as templates
            - Avoid next action suggestions similar to ones that were dismissed
            - Reference past project completion patterns for milestone suggestions
            - Consider goal linking patterns that worked for similar projects
            """;
    }

    public static string BuildUserPrompt(
        SituationalAssessment assessment,
        IReadOnlyList<InterventionPlanItem> projectInterventions,
        IReadOnlyList<ProjectSnapshot> projects,
        IReadOnlyList<TaskSnapshot> tasks,
        IReadOnlyList<GoalSnapshot> goals,
        DateOnly today,
        SeasonSnapshot? season = null,
        IReadOnlyList<UserRoleSnapshot>? roles = null,
        IReadOnlyList<UserValueSnapshot>? values = null,
        RagContext? ragContext = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Assessment Summary: {assessment.CapacityStatus} capacity, {assessment.OverallMomentum} momentum");
        sb.AppendLine($"Today: {today:yyyy-MM-dd} ({today.DayOfWeek})");

        if (season is not null)
        {
            sb.AppendLine($"Season: {season.Label} ({season.Type}, intensity {season.Intensity}/10)");
            if (season.FocusGoalIds.Count > 0)
                sb.AppendLine($"Focus goals: {string.Join(", ", season.FocusGoalIds)}");
        }
        sb.AppendLine();

        sb.AppendLine("# Intervention Plan (your assignments):");
        foreach (var item in projectInterventions)
        {
            sb.AppendLine($"- [{item.Priority}] {item.Area}: {item.Reasoning}");
            sb.AppendLine($"  Target type: {item.TargetType}");
            if (item.TargetEntityIds is { Count: > 0 })
                sb.AppendLine($"  Target entities: {string.Join(", ", item.TargetEntityIds)}");
        }
        sb.AppendLine();

        // Active projects
        var activeProjects = projects
            .Where(p => p.Status == ProjectStatus.Active)
            .OrderBy(p => p.NextTaskId.HasValue ? 1 : 0) // Stuck projects first
            .ToList();

        sb.AppendLine($"# Active Projects ({activeProjects.Count})");
        foreach (var p in activeProjects)
        {
            var stuck = p.NextTaskId is null ? " [STUCK - no next action]" : "";
            var linked = p.GoalId.HasValue ? $"Goal:{p.GoalId}" : "No goal";
            var deadline = p.TargetEndDate?.ToString("MMM dd") ?? "no deadline";
            var milestones = p.Milestones?.Count > 0
                ? $" | {p.Milestones.Count(m => m.Status == "Completed")}/{p.Milestones.Count} milestones"
                : "";
            sb.AppendLine($"- [{p.Id}] \"{p.Title}\" | P{p.Priority} | {p.CompletedTasks}/{p.TotalTasks} tasks | {deadline} | {linked}{milestones}{stuck}");
        }
        sb.AppendLine();

        // All incomplete tasks (for context and next action suggestions)
        var allIncompleteTasks = tasks
            .Where(t => t.Status != TaskStatus.Completed &&
                        t.Status != TaskStatus.Cancelled &&
                        t.Status != TaskStatus.Archived)
            .OrderBy(t => t.Priority)
            .ToList();

        // Unscheduled tasks are preferred for next action
        var unscheduledTasks = allIncompleteTasks.Where(t => t.ScheduledDate is null).ToList();

        // Group all project tasks by project (including scheduled)
        var tasksByProject = allIncompleteTasks
            .Where(t => t.ProjectId.HasValue)
            .GroupBy(t => t.ProjectId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        if (tasksByProject.Count > 0)
        {
            sb.AppendLine("# Project Tasks (for next action suggestions)");
            sb.AppendLine("NOTE: Prefer unscheduled tasks for suggestedNextTaskId, but scheduled tasks can also be considered.");
            foreach (var kvp in tasksByProject.Take(10))
            {
                var projectTitle = projects.FirstOrDefault(p => p.Id == kvp.Key)?.Title ?? "Unknown";
                sb.AppendLine($"## Project: {projectTitle} ({kvp.Key})");
                foreach (var t in kvp.Value.Take(8))
                {
                    var scheduledStatus = t.ScheduledDate is null ? "unscheduled" : $"scheduled:{t.ScheduledDate:MMM dd}";
                    sb.AppendLine($"  - [{t.Id}] \"{t.Title}\" | P{t.Priority} | {t.Status} | {scheduledStatus} | Est:{t.EstMinutes ?? 0}min | Energy:{t.EnergyLevel}");
                }
            }
            sb.AppendLine();
        }

        // Goals for project linking (include Draft for feedback)
        var activeGoals = goals
            .Where(g => g.Status == GoalStatus.Active || g.Status == GoalStatus.Draft)
            .OrderBy(g => g.Priority)
            .ToList();

        if (activeGoals.Count > 0)
        {
            sb.AppendLine($"# Goals ({activeGoals.Count}) - for project linking");
            foreach (var g in activeGoals.Take(5))
            {
                var deadline = g.Deadline?.ToString("MMM dd") ?? "no deadline";
                sb.AppendLine($"- [{g.Id}] \"{g.Title}\" | Status:{g.Status} | P{g.Priority} | {deadline}");
            }
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

        // Stale projects (potential archive candidates)
        var staleProjects = projects
            .Where(p => p.Status is ProjectStatus.Active or ProjectStatus.Paused &&
                        p.TotalTasks == 0)
            .ToList();

        if (staleProjects.Count > 0)
        {
            sb.AppendLine($"# Potentially Stale Projects ({staleProjects.Count}) - archive candidates");
            foreach (var p in staleProjects)
            {
                sb.AppendLine($"- [{p.Id}] \"{p.Title}\" | {p.Status} | 0 tasks");
            }
            sb.AppendLine();
        }

        // Unlinked projects (candidates for goal linking)
        var unlinkedProjects = activeProjects.Where(p => p.GoalId is null).ToList();
        if (unlinkedProjects.Count > 0 && activeGoals.Count > 0)
        {
            sb.AppendLine($"# Unlinked Projects ({unlinkedProjects.Count}) - consider linking to goals via ProjectGoalLinkSuggestion");
            sb.AppendLine("These projects have NO GoalId set. Consider suggesting a link if a goal clearly relates.");
            foreach (var p in unlinkedProjects)
            {
                sb.AppendLine($"- [{p.Id}] \"{p.Title}\" | P{p.Priority} | {p.TargetEndDate?.ToString("MMM dd") ?? "no deadline"} | NO GOAL LINKED");
            }
            sb.AppendLine();
        }

        // Add critical constraints to prevent hallucinated IDs
        sb.AppendLine("## CRITICAL CONSTRAINTS");
        sb.AppendLine("- For Update/Remove actions, you MUST use IDs from the lists above.");
        if (activeProjects.Count > 0)
            sb.AppendLine($"- VALID PROJECT IDS: {string.Join(", ", activeProjects.Select(p => p.Id))}");
        else
            sb.AppendLine("- NO PROJECTS EXIST. Only generate ProjectSuggestion (Create) with targetEntityId: null.");

        // Task constraints for ProjectStuckFix
        var projectTaskIds = tasksByProject.SelectMany(kvp => kvp.Value).Select(t => t.Id).ToList();
        if (projectTaskIds.Count > 0)
        {
            sb.AppendLine($"- VALID TASK IDS (for suggestedNextTaskId): {string.Join(", ", projectTaskIds)}");
            sb.AppendLine("- PREFER suggestedNextTaskId over suggestedNewTask when an existing task fits the project");
            sb.AppendLine("- Only create suggestedNewTask if existing tasks are too large, wrong scope, or blocked");
        }
        else
            sb.AppendLine("- NO PROJECT TASKS EXIST. For ProjectStuckFix, use suggestedNewTask instead of suggestedNextTaskId.");

        if (activeGoals.Count > 0)
            sb.AppendLine($"- VALID GOAL IDS (for goalId): {string.Join(", ", activeGoals.Select(g => g.Id))}");
        else
            sb.AppendLine("- NO GOALS EXIST. Set goalId to null in ProjectSuggestion payloads.");
        sb.AppendLine("- Do NOT invent or hallucinate entity IDs. Only use IDs that appear in the lists above.");
        sb.AppendLine();

        // Add RAG historical context BEFORE generating - inform recommendations
        RagContextFormatter.AppendForGeneration(sb, ragContext, "Project", today);

        sb.AppendLine("Generate recommendations for each intervention plan item assigned to you.");

        return sb.ToString();
    }
}

using System.Text;
using Mastery.Application.Common.Models;
using Mastery.Domain.Enums;

namespace Mastery.Infrastructure.Services.Prompts;

/// <summary>
/// Stage 3 (Goal + Metric + Project Domain): Generates goal, metric, and project recommendations.
/// Handles: GoalScoreboardSuggestion, MetricObservationReminder, ProjectStuckFix
/// </summary>
internal static class GoalMetricGenerationPrompt
{
    public const string PromptVersion = "goal-metric-gen-v3.0";
    public const string Model = "gpt-5-mini";
    public const string SchemaName = "goal_metric_generation";

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
                      "type": { "type": "string", "enum": ["GoalScoreboardSuggestion"] },
                      "targetKind": { "type": "string", "enum": ["Goal"] },
                      "targetEntityId": { "type": "string" },
                      "targetEntityTitle": { "type": "string" },
                      "actionKind": { "type": "string", "enum": ["Update"] },
                      "title": { "type": "string" },
                      "rationale": { "type": "string" },
                      "score": { "type": "number" },
                      "actionPayload": {
                        "type": "object",
                        "properties": {
                          "goalId": { "type": "string" },

                          "existingMetricDefinitionId": { "anyOf": [{ "type": "string" }, { "type": "null" }] },

                          "newMetricName": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                          "newMetricDescription": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                          "newMetricDataType": { "type": "string", "enum": ["Number", "Boolean", "Duration", "Percentage", "Count", "Rating"] },
                          "newMetricDirection": { "type": "string", "enum": ["Increase", "Decrease", "Maintain"] },
                          "newMetricUnitType": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                          "newMetricUnitDisplayLabel": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                          "newMetricDefaultCadence": { "type": "string", "enum": ["Daily", "Weekly", "Monthly", "Rolling"] },
                          "newMetricDefaultAggregation": { "type": "string", "enum": ["Sum", "Average", "Max", "Min", "Count", "Latest"] },

                          "kind": { "type": "string", "enum": ["Lag", "Lead", "Constraint"] },

                          "targetType": { "type": "string", "enum": ["AtLeast", "AtMost", "Between", "Exactly"] },
                          "targetValue": { "type": "number" },
                          "targetMaxValue": { "anyOf": [{ "type": "number" }, { "type": "null" }] },

                          "windowType": { "type": "string", "enum": ["Daily", "Weekly", "Monthly", "Rolling"] },
                          "rollingDays": { "anyOf": [{ "type": "integer" }, { "type": "null" }] },

                          "aggregation": { "type": "string", "enum": ["Sum", "Average", "Max", "Min", "Count", "Latest"] },
                          "sourceHint": { "type": "string", "enum": ["Manual", "Habit", "Task", "CheckIn", "Integration", "Computed"] },

                          "weight": { "type": "number" },
                          "baseline": { "anyOf": [{ "type": "number" }, { "type": "null" }] },

                          "_summary": { "type": "string" }
                        },
                        "required": ["goalId", "existingMetricDefinitionId", "newMetricName", "newMetricDescription",
                                     "newMetricDataType", "newMetricDirection", "newMetricUnitType", "newMetricUnitDisplayLabel",
                                     "newMetricDefaultCadence", "newMetricDefaultAggregation",
                                     "kind", "targetType", "targetValue", "targetMaxValue",
                                     "windowType", "rollingDays", "aggregation", "sourceHint",
                                     "weight", "baseline", "_summary"],
                        "additionalProperties": false
                      }
                    },
                    "required": ["type", "targetKind", "targetEntityId", "targetEntityTitle", "actionKind", "title", "rationale", "score", "actionPayload"],
                    "additionalProperties": false
                  },
                  {
                    "type": "object",
                    "properties": {
                      "type": { "type": "string", "enum": ["MetricObservationReminder"] },
                      "targetKind": { "type": "string", "enum": ["Metric"] },
                      "targetEntityId": { "type": "string" },
                      "targetEntityTitle": { "type": "string" },
                      "actionKind": { "type": "string", "enum": ["Create"] },
                      "title": { "type": "string" },
                      "rationale": { "type": "string" },
                      "score": { "type": "number" },
                      "actionPayload": {
                        "type": "object",
                        "properties": {
                          "metricDefinitionId": { "type": "string" },
                          "metricName": { "type": "string" },
                          "_summary": { "type": "string" }
                        },
                        "required": ["metricDefinitionId", "metricName", "_summary"],
                        "additionalProperties": false
                      }
                    },
                    "required": ["type", "targetKind", "targetEntityId", "targetEntityTitle", "actionKind", "title", "rationale", "score", "actionPayload"],
                    "additionalProperties": false
                  },
                  {
                    "type": "object",
                    "properties": {
                      "type": { "type": "string", "enum": ["ProjectStuckFix"] },
                      "targetKind": { "type": "string", "enum": ["Project"] },
                      "targetEntityId": { "type": "string" },
                      "targetEntityTitle": { "type": "string" },
                      "actionKind": { "type": "string", "enum": ["Create"] },
                      "title": { "type": "string" },
                      "rationale": { "type": "string" },
                      "score": { "type": "number" },
                      "actionPayload": {
                        "type": "object",
                        "properties": {
                          "title": { "type": "string" },
                          "estMinutes": { "type": "integer" },
                          "energyCost": { "type": "integer" },
                          "priority": { "type": "integer" },
                          "projectId": { "type": "string" },
                          "startAsReady": { "type": "boolean" },
                          "_summary": { "type": "string" }
                        },
                        "required": ["title", "estMinutes", "energyCost", "priority", "projectId", "startAsReady", "_summary"],
                        "additionalProperties": false
                      }
                    },
                    "required": ["type", "targetKind", "targetEntityId", "targetEntityTitle", "actionKind", "title", "rationale", "score", "actionPayload"],
                    "additionalProperties": false
                  },
                  {
                    "type": "object",
                    "properties": {
                      "type": { "type": "string", "enum": ["GoalEditSuggestion"] },
                      "targetKind": { "type": "string", "enum": ["Goal"] },
                      "targetEntityId": { "type": "string" },
                      "targetEntityTitle": { "type": "string" },
                      "actionKind": { "type": "string", "enum": ["Update"] },
                      "title": { "type": "string" },
                      "rationale": { "type": "string" },
                      "score": { "type": "number" },
                      "actionPayload": {
                        "type": "object",
                        "properties": {
                          "goalId": { "type": "string" },
                          "newTitle": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                          "newPriority": { "anyOf": [{ "type": "integer" }, { "type": "null" }] },
                          "newDeadline": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                          "_summary": { "type": "string" }
                        },
                        "required": ["goalId", "newTitle", "newPriority", "newDeadline", "_summary"],
                        "additionalProperties": false
                      }
                    },
                    "required": ["type", "targetKind", "targetEntityId", "targetEntityTitle", "actionKind", "title", "rationale", "score", "actionPayload"],
                    "additionalProperties": false
                  },
                  {
                    "type": "object",
                    "properties": {
                      "type": { "type": "string", "enum": ["GoalArchiveSuggestion"] },
                      "targetKind": { "type": "string", "enum": ["Goal"] },
                      "targetEntityId": { "type": "string" },
                      "targetEntityTitle": { "type": "string" },
                      "actionKind": { "type": "string", "enum": ["Remove"] },
                      "title": { "type": "string" },
                      "rationale": { "type": "string" },
                      "score": { "type": "number" },
                      "actionPayload": {
                        "type": "object",
                        "properties": {
                          "goalId": { "type": "string" },
                          "_summary": { "type": "string" }
                        },
                        "required": ["goalId", "_summary"],
                        "additionalProperties": false
                      }
                    },
                    "required": ["type", "targetKind", "targetEntityId", "targetEntityTitle", "actionKind", "title", "rationale", "score", "actionPayload"],
                    "additionalProperties": false
                  },
                  {
                    "type": "object",
                    "properties": {
                      "type": { "type": "string", "enum": ["MetricEditSuggestion"] },
                      "targetKind": { "type": "string", "enum": ["Metric"] },
                      "targetEntityId": { "type": "string" },
                      "targetEntityTitle": { "type": "string" },
                      "actionKind": { "type": "string", "enum": ["Update"] },
                      "title": { "type": "string" },
                      "rationale": { "type": "string" },
                      "score": { "type": "number" },
                      "actionPayload": {
                        "type": "object",
                        "properties": {
                          "metricId": { "type": "string" },
                          "newName": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                          "newDirection": { "anyOf": [{ "type": "string", "enum": ["Increase", "Decrease"] }, { "type": "null" }] },
                          "_summary": { "type": "string" }
                        },
                        "required": ["metricId", "newName", "newDirection", "_summary"],
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
                          "newPriority": { "anyOf": [{ "type": "integer" }, { "type": "null" }] },
                          "_summary": { "type": "string" }
                        },
                        "required": ["projectId", "newTitle", "newPriority", "_summary"],
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
            You are generating goal, metric, and project recommendations for the Mastery personal development system.
            You will receive a situational assessment, intervention plan items, and relevant entity data.

            For each intervention plan item assigned to you, generate ONE recommendation with a complete actionPayload.

            ## Recommendation Types You Handle

            ### GoalScoreboardSuggestion (ActionKind: Update)
            Add a specific metric to a goal's scoreboard.

            CHOOSING BETWEEN EXISTING VS NEW METRIC:
            - Check the "Available Metrics" list first. If one fits, use existingMetricDefinitionId
            - Only create a new metric if nothing appropriate exists
            - When creating: use specific, measurable names (e.g., "Daily Deep Work Minutes" not "Work Time")

            actionPayload fields:
            === METRIC DEFINITION SELECTION (use existing OR create new) ===
            - goalId: The goal's GUID (from the Goals list)
            - existingMetricDefinitionId: GUID from Available Metrics list (set to null if creating new)

            === NEW METRIC DEFINITION FIELDS (only if creating new metric) ===
            These define WHAT is being measured (stored in MetricDefinition entity):
            - newMetricName: Name for new metric (set to null if using existing)
            - newMetricDescription: Brief description (set to null if using existing)
            - newMetricDataType: Number | Boolean | Duration | Percentage | Count | Rating
            - newMetricDirection: Increase | Decrease | Maintain
            - newMetricUnitType: Unit category (duration/count/percentage/rating/weight/currency/none)
            - newMetricUnitDisplayLabel: Display symbol (min, %, $, /5, etc.)
            - newMetricDefaultCadence: Daily | Weekly | Monthly | Rolling
            - newMetricDefaultAggregation: Sum | Average | Max | Min | Count | Latest

            === GOAL METRIC BINDING FIELDS (always required) ===
            These define HOW the metric is used in this goal's scoreboard (stored in GoalMetric entity):
            - kind: Lead (predictive behavior) | Lag (outcome) | Constraint (guardrail)
            - targetType: AtLeast | AtMost | Between | Exactly
            - targetValue: Numeric target (or min for Between)
            - targetMaxValue: Max value (only for Between, else null)
            - windowType: Daily | Weekly | Monthly | Rolling
            - rollingDays: Number of days (only for Rolling, else null)
            - aggregation: Sum | Average | Max | Min | Count | Latest
            - sourceHint: Manual | Habit | Task | CheckIn | Integration | Computed
            - weight: 0.0-1.0 (importance in goal health calculation, default 1.0)
            - baseline: Starting value for progress calculation (null if not known)
            - _summary: Human-readable summary, e.g., "Add 'Daily Steps' as Lead metric (>= 8000/week)"

            ## Field Reference - MetricDefinition Fields (WHAT is being measured)
            Only populate these (newMetric*) when creating a NEW metric definition.
            """ + SchemaReference.MetricDataTypeSchema + """

            """ + SchemaReference.MetricDirectionSchema + """

            """ + SchemaReference.MetricUnitGuidance + """

            ## Field Reference - GoalMetric Binding Fields (HOW metric is used in goal)
            Always populate these - they define the goal-specific configuration.
            """ + SchemaReference.MetricKindSchema + """

            """ + SchemaReference.TargetTypeSchema + """

            """ + SchemaReference.WindowTypeSchema + """

            """ + SchemaReference.MetricAggregationSchema + """

            """ + SchemaReference.MetricSourceTypeSchema + """

            """ + SchemaReference.GoalMetricFieldGuidance + """

            ## Multi-Entity Creation
            When adding a metric to a goal, the system needs both:
            1. MetricDefinition - The reusable definition of what is measured (can be shared across goals)
            2. GoalMetric - The goal-specific binding with target, window, weight, etc.

            If using existingMetricDefinitionId: Only the GoalMetric is created (the definition already exists)
            If creating new: Both MetricDefinition AND GoalMetric are created (MetricDefinition first)

            METRIC TYPE GUIDELINES:
            - Lag metrics: Outcomes you're trying to achieve (weight, revenue, project completion %)
            - Lead metrics: Predictive behaviors that drive outcomes (workout sessions, cold calls, study hours)
            - Constraint metrics: Guardrails to not sacrifice (sleep hours, stress level, family time)

            TARGET GUIDELINES:
            - Set REALISTIC targets based on the goal context
            - For habits → typically Count aggregation with AtLeast target
            - For progress metrics → typically Sum or Average with AtLeast/AtMost
            - For guardrails → typically AtLeast (min sleep) or AtMost (max stress)

            ### MetricObservationReminder (ActionKind: Create)
            Remind the user to record a metric observation that's stale or never recorded.
            actionPayload: { "metricDefinitionId": "guid-string", "metricName": "string", "_summary": "Record 'Weight' observation (7 days stale)" }
            Note: This creates a reminder/prompt, not the observation itself.

            ### ProjectStuckFix (ActionKind: Create)
            Create a next-action task for a project that has no next action defined.
            actionPayload: {
              "title": "string (a concrete, small next step)",
              "estMinutes": number (15-60, keep it small to unblock),
              "energyCost": number (1-5),
              "priority": number (1-10),
              "projectId": "guid-string",
              "startAsReady": true,
              "_summary": "Create task 'Define requirements' for 'API Project'"
            }

            ### GoalEditSuggestion (ActionKind: Update)
            Suggest modifying an existing goal's properties.
            actionPayload: {
              "goalId": "guid-string (required)",
              "newTitle": "string (optional)",
              "newPriority": integer (optional, 1-5),
              "newDeadline": "string ISO date (optional)",
              "_summary": "Extend deadline to March 15"
            }

            ### GoalArchiveSuggestion (ActionKind: Remove)
            Suggest archiving a goal that's completed or no longer relevant.
            actionPayload: { "goalId": "guid-string", "_summary": "Archive completed goal" }

            ### MetricEditSuggestion (ActionKind: Update)
            Suggest modifying a metric definition.
            actionPayload: {
              "metricId": "guid-string (required)",
              "newName": "string (optional)",
              "newDirection": "Increase" | "Decrease" (optional),
              "_summary": "Rename metric to 'Daily Steps'"
            }

            ### ProjectEditSuggestion (ActionKind: Update)
            Suggest modifying an existing project.
            actionPayload: {
              "projectId": "guid-string (required)",
              "newTitle": "string (optional)",
              "newPriority": integer (optional, 1-5),
              "_summary": "Increase priority to P1"
            }

            ### ProjectArchiveSuggestion (ActionKind: Remove)
            Suggest archiving a stale or completed project.
            actionPayload: { "projectId": "guid-string", "_summary": "Archive stale project (no activity for 30 days)" }

            ## Guidelines
            - For ProjectStuckFix: suggest the SMALLEST possible next step to unblock
            - For GoalScoreboardSuggestion: explain which metric kinds are missing and why they matter
            - For MetricObservationReminder: note how long since last observation
            - For Edit suggestions: only include fields that need to change
            - For Archive suggestions: use when entities are completed, stale, or no longer aligned with priorities
            - Connect everything back to the user's goals and momentum
            - ALWAYS include _summary in actionPayload - this is shown to the user before they accept
            - _summary should be concise (under 60 chars) and describe exactly what will happen
            """;
    }

    public static string BuildUserPrompt(
        SituationalAssessment assessment,
        IReadOnlyList<InterventionPlanItem> goalMetricInterventions,
        IReadOnlyList<GoalSnapshot> goals,
        IReadOnlyList<ProjectSnapshot> projects,
        IReadOnlyList<MetricDefinitionSnapshot> metrics,
        IReadOnlyList<UserValueSnapshot>? values = null,
        IReadOnlyList<UserRoleSnapshot>? roles = null,
        SeasonSnapshot? season = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Assessment: {assessment.OverallMomentum} momentum");
        sb.AppendLine();

        // Core values for goal alignment suggestions
        if (values is { Count: > 0 })
        {
            sb.AppendLine("# Core Values (goals should align)");
            foreach (var v in values.Take(5))
                sb.AppendLine($"  - {v.Label}");
            sb.AppendLine();
        }

        // User roles for context
        if (roles is { Count: > 0 })
        {
            sb.AppendLine("# User Roles (for alignment context)");
            foreach (var r in roles.Where(r => r.IsActive).Take(5))
                sb.AppendLine($"  - [{r.Id}] \"{r.Label}\" | SeasonPriority:{r.SeasonPriority}");
            sb.AppendLine();
        }

        // Season focus goals for prioritization
        if (season?.FocusGoalIds.Count > 0)
        {
            var focusGoalTitles = goals
                .Where(g => season.FocusGoalIds.Contains(g.Id))
                .Select(g => g.Title)
                .ToList();
            if (focusGoalTitles.Count > 0)
            {
                sb.AppendLine($"# Season Focus Goals (prioritize interventions for these)");
                foreach (var title in focusGoalTitles)
                    sb.AppendLine($"  - \"{title}\"");
                sb.AppendLine();
            }
        }

        sb.AppendLine("# Intervention Plan (your assignments):");
        foreach (var item in goalMetricInterventions)
        {
            sb.AppendLine($"- [{item.Priority}] {item.Area}: {item.Reasoning}");
            sb.AppendLine($"  Target type: {item.TargetType}");
            if (item.TargetEntityIds is { Count: > 0 })
                sb.AppendLine($"  Target entities: {string.Join(", ", item.TargetEntityIds)}");
        }
        sb.AppendLine();

        var activeGoals = goals.Where(g => g.Status == GoalStatus.Active).ToList();
        sb.AppendLine($"# Active Goals ({activeGoals.Count})");
        foreach (var g in activeGoals)
        {
            sb.AppendLine($"- [{g.Id}] \"{g.Title}\" | Priority:{g.Priority}");
            var metricKinds = g.Metrics.Select(m => m.Kind).Distinct().ToList();
            var missing = new List<string>();
            if (!metricKinds.Contains(MetricKind.Lag)) missing.Add("Lag");
            if (!metricKinds.Contains(MetricKind.Lead)) missing.Add("Lead");
            if (!metricKinds.Contains(MetricKind.Constraint)) missing.Add("Constraint");
            if (missing.Count > 0)
                sb.AppendLine($"  Missing metric kinds: {string.Join(", ", missing)}");
            sb.AppendLine($"  === Current GoalMetric Bindings ===");
            foreach (var m in g.Metrics)
            {
                sb.AppendLine($"  - {m.Kind}: \"{m.MetricName}\"");
                sb.AppendLine($"    Target: {m.TargetType} {m.TargetValue}" +
                    (m.TargetMaxValue.HasValue ? $"-{m.TargetMaxValue}" : ""));
                sb.AppendLine($"    Window: {m.WindowType}" +
                    (m.RollingDays.HasValue ? $" ({m.RollingDays} days)" : ""));
                sb.AppendLine($"    Aggregation: {m.Aggregation} | Weight: {m.Weight:F1} | Source: {m.SourceHint}");
                sb.AppendLine($"    Current: {m.CurrentValue?.ToString() ?? "?"} | Baseline: {m.Baseline?.ToString() ?? "not set"}");
            }
        }
        sb.AppendLine();

        var activeProjects = projects.Where(p => p.Status == ProjectStatus.Active).ToList();
        if (activeProjects.Count > 0)
        {
            sb.AppendLine($"# Active Projects ({activeProjects.Count})");
            foreach (var p in activeProjects)
            {
                var stuck = p.NextTaskId is null ? "STUCK(no next action)" : "HasNextAction";
                sb.AppendLine($"- [{p.Id}] \"{p.Title}\" | {stuck} | Progress:{p.CompletedTasks}/{p.TotalTasks}");
            }
            sb.AppendLine();
        }

        // Show ALL metric definitions from user's library with full details
        if (metrics.Count > 0)
        {
            sb.AppendLine($"# Available Metric Definitions ({metrics.Count} in user's library)");
            sb.AppendLine("(Use existingMetricDefinitionId when one of these fits; populate newMetric* fields only if creating new)");
            sb.AppendLine();
            foreach (var m in metrics)
            {
                var alreadyOnGoal = goals.Any(g => g.Metrics.Any(gm => gm.MetricDefinitionId == m.Id));
                var status = alreadyOnGoal ? "[ALREADY ON A GOAL]" : "[AVAILABLE]";
                sb.AppendLine($"- [{m.Id}] {status}");
                sb.AppendLine($"  Name: \"{m.Name}\"");
                if (!string.IsNullOrEmpty(m.Description))
                    sb.AppendLine($"  Description: \"{m.Description}\"");
                sb.AppendLine($"  === MetricDefinition Properties ===");
                sb.AppendLine($"  DataType: {m.DataType} | Direction: {m.Direction}");
                sb.AppendLine($"  Unit: {m.UnitDisplayLabel ?? "none"} ({m.UnitType ?? "none"})");
                sb.AppendLine($"  Defaults: {m.DefaultCadence} cadence, {m.DefaultAggregation} aggregation");
                if (m.Tags.Count > 0)
                    sb.AppendLine($"  Tags: {string.Join(", ", m.Tags)}");
                var lastObs = m.LastObservationDate?.ToString("yyyy-MM-dd") ?? "never recorded";
                sb.AppendLine($"  Last observation: {lastObs}");
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("# No Metric Definitions exist yet - you'll need to create new ones.");
            sb.AppendLine();
        }

        var staleMetrics = metrics
            .Where(m => m.SourceType == MetricSourceType.Manual &&
                       (m.LastObservationDate is null || m.LastObservationDate < DateTime.UtcNow.AddDays(-7)))
            .ToList();
        if (staleMetrics.Count > 0)
        {
            sb.AppendLine($"# Stale/Unrecorded Metrics ({staleMetrics.Count})");
            foreach (var m in staleMetrics)
            {
                var lastObs = m.LastObservationDate?.ToString("yyyy-MM-dd") ?? "never recorded";
                sb.AppendLine($"- [{m.Id}] \"{m.Name}\" | Last:{lastObs}");
            }
            sb.AppendLine();
        }

        // Add critical constraints to prevent hallucinated IDs
        sb.AppendLine("## CRITICAL CONSTRAINTS");
        sb.AppendLine("- For Update/Remove actions on Goals/Metrics/Projects, you MUST use IDs from the lists above.");
        if (activeGoals.Count > 0)
            sb.AppendLine($"- VALID GOAL IDS: {string.Join(", ", activeGoals.Select(g => g.Id))}");
        else
            sb.AppendLine("- NO GOALS EXIST. Do NOT generate GoalScoreboardSuggestion, GoalEditSuggestion, or GoalArchiveSuggestion.");
        if (activeProjects.Count > 0)
            sb.AppendLine($"- VALID PROJECT IDS: {string.Join(", ", activeProjects.Select(p => p.Id))}");
        else
            sb.AppendLine("- NO PROJECTS EXIST. Do NOT generate ProjectStuckFix, ProjectEditSuggestion, or ProjectArchiveSuggestion.");
        if (metrics.Count > 0)
        {
            sb.AppendLine($"- VALID METRIC DEFINITION IDS (for existingMetricDefinitionId): {string.Join(", ", metrics.Select(m => m.Id))}");
            sb.AppendLine("- For GoalScoreboardSuggestion: prefer using existingMetricDefinitionId from the list above; only create new if none fit.");
        }
        else
        {
            sb.AppendLine("- NO METRICS EXIST. For GoalScoreboardSuggestion, you must create new metrics (use newMetricName/newMetricDescription).");
            sb.AppendLine("- Do NOT generate MetricObservationReminder or MetricEditSuggestion when no metrics exist.");
        }
        sb.AppendLine("- Do NOT invent or hallucinate entity IDs. Only use IDs that appear in the lists above.");
        sb.AppendLine();

        sb.AppendLine("Generate recommendations for each intervention plan item assigned to you.");
        return sb.ToString();
    }
}

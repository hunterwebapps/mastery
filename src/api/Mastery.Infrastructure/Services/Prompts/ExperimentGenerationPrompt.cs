using System.Text;
using Mastery.Application.Common.Models;
using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Enums;

namespace Mastery.Infrastructure.Services.Prompts;

/// <summary>
/// Stage 3 (Experiment + Reflection Domain): Generates experiments and reflection prompts.
/// Handles: ExperimentRecommendation, CheckInConsistencyNudge
/// </summary>
internal static class ExperimentGenerationPrompt
{
    public const string PromptVersion = "experiment-gen-v2.0";
    public const string Model = "gpt-5-mini";
    public const string SchemaName = "experiment_generation";

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
                      "type": { "type": "string", "enum": ["ExperimentRecommendation"] },
                      "targetKind": { "type": "string", "enum": ["Experiment"] },
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
                          "category": {
                            "type": "string",
                            "enum": ["Habit", "Routine", "Environment", "Mindset", "Productivity",
                                     "Health", "Social", "PlanRealism", "FrictionReduction",
                                     "CheckInConsistency", "Top1FollowThrough", "Other"]
                          },
                          "hypothesis": {
                            "type": "object",
                            "properties": {
                              "change": { "type": "string" },
                              "expectedOutcome": { "type": "string" },
                              "rationale": { "anyOf": [{ "type": "string" }, { "type": "null" }] }
                            },
                            "required": ["change", "expectedOutcome", "rationale"],
                            "additionalProperties": false
                          },
                          "linkedGoalIds": {
                            "type": "array",
                            "items": { "type": "string" }
                          },
                          "measurementPlan": {
                            "type": "object",
                            "properties": {
                              "primaryMetricDefinitionId": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                              "newPrimaryMetric": {
                                "anyOf": [{
                                  "type": "object",
                                  "properties": {
                                    "name": { "type": "string" },
                                    "description": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                                    "dataType": { "type": "string", "enum": ["Number", "Boolean", "Duration", "Percentage", "Count", "Rating"] },
                                    "direction": { "type": "string", "enum": ["Increase", "Decrease", "Maintain"] },
                                    "unitType": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                                    "unitDisplayLabel": { "anyOf": [{ "type": "string" }, { "type": "null" }] }
                                  },
                                  "required": ["name", "description", "dataType", "direction", "unitType", "unitDisplayLabel"],
                                  "additionalProperties": false
                                }, { "type": "null" }]
                              },
                              "primaryAggregation": { "type": "string", "enum": ["Sum", "Average", "Max", "Min", "Count", "Latest"] },
                              "baselineWindowDays": { "type": "integer" },
                              "runWindowDays": { "type": "integer" },
                              "guardrailMetricDefinitionIds": {
                                "type": "array",
                                "items": { "type": "string" }
                              }
                            },
                            "required": ["primaryMetricDefinitionId", "newPrimaryMetric", "primaryAggregation",
                                         "baselineWindowDays", "runWindowDays", "guardrailMetricDefinitionIds"],
                            "additionalProperties": false
                          },
                          "_summary": { "type": "string" }
                        },
                        "required": ["title", "description", "category", "hypothesis", "linkedGoalIds",
                                     "measurementPlan", "_summary"],
                        "additionalProperties": false
                      }
                    },
                    "required": ["type", "targetKind", "targetEntityId", "targetEntityTitle", "actionKind", "title", "rationale", "score", "actionPayload"],
                    "additionalProperties": false
                  },
                  {
                    "type": "object",
                    "properties": {
                      "type": { "type": "string", "enum": ["CheckInConsistencyNudge"] },
                      "targetKind": { "type": "string", "enum": ["UserProfile"] },
                      "targetEntityId": { "type": "null" },
                      "targetEntityTitle": { "type": "string" },
                      "actionKind": { "type": "string", "enum": ["ReflectPrompt"] },
                      "title": { "type": "string" },
                      "rationale": { "type": "string" },
                      "score": { "type": "number" },
                      "actionPayload": {
                        "type": "object",
                        "properties": {
                          "prompt": { "type": "string" },
                          "_summary": { "type": "string" }
                        },
                        "required": ["prompt", "_summary"],
                        "additionalProperties": false
                      }
                    },
                    "required": ["type", "targetKind", "targetEntityId", "targetEntityTitle", "actionKind", "title", "rationale", "score", "actionPayload"],
                    "additionalProperties": false
                  },
                  {
                    "type": "object",
                    "properties": {
                      "type": { "type": "string", "enum": ["ExperimentEditSuggestion"] },
                      "targetKind": { "type": "string", "enum": ["Experiment"] },
                      "targetEntityId": { "type": "string" },
                      "targetEntityTitle": { "type": "string" },
                      "actionKind": { "type": "string", "enum": ["Update"] },
                      "title": { "type": "string" },
                      "rationale": { "type": "string" },
                      "score": { "type": "number" },
                      "actionPayload": {
                        "type": "object",
                        "properties": {
                          "experimentId": { "type": "string" },
                          "newTitle": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                          "newDescription": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                          "_summary": { "type": "string" }
                        },
                        "required": ["experimentId", "newTitle", "newDescription", "_summary"],
                        "additionalProperties": false
                      }
                    },
                    "required": ["type", "targetKind", "targetEntityId", "targetEntityTitle", "actionKind", "title", "rationale", "score", "actionPayload"],
                    "additionalProperties": false
                  },
                  {
                    "type": "object",
                    "properties": {
                      "type": { "type": "string", "enum": ["ExperimentArchiveSuggestion"] },
                      "targetKind": { "type": "string", "enum": ["Experiment"] },
                      "targetEntityId": { "type": "string" },
                      "targetEntityTitle": { "type": "string" },
                      "actionKind": { "type": "string", "enum": ["Remove"] },
                      "title": { "type": "string" },
                      "rationale": { "type": "string" },
                      "score": { "type": "number" },
                      "actionPayload": {
                        "type": "object",
                        "properties": {
                          "experimentId": { "type": "string" },
                          "reason": { "anyOf": [{ "type": "string" }, { "type": "null" }] },
                          "_summary": { "type": "string" }
                        },
                        "required": ["experimentId", "reason", "_summary"],
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
            You are generating experiment and reflection recommendations for the Mastery personal development system.
            You will receive a situational assessment, intervention plan items, and user context.

            For each intervention plan item assigned to you, generate ONE recommendation with a complete actionPayload.

            ## Recommendation Types You Handle

            ### ExperimentRecommendation (ActionKind: Create)
            Suggest a behavioral experiment to test a hypothesis about a recurring pattern.
            targetEntityTitle: "New Experiment"
            actionPayload: {
              "title": "string (e.g., 'Sleep by 10pm for 2 weeks')",
              "description": "string or null (fuller description)",
              "category": (see ExperimentCategory below),
              "hypothesis": {
                "change": "string (what the user will do differently)",
                "expectedOutcome": "string (what improvement is expected)",
                "rationale": "string or null (why this change should work)"
              },
              "linkedGoalIds": ["guid-string", ...] (goals this experiment impacts),
              "measurementPlan": {
                "primaryMetricDefinitionId": "guid-string or null (use existing metric, or null to create new)",
                "newPrimaryMetric": { (only if creating new metric)
                  "name": "string",
                  "description": "string or null",
                  "dataType": "Number" | "Boolean" | "Duration" | "Percentage" | "Count" | "Rating",
                  "direction": "Increase" | "Decrease" | "Maintain",
                  "unitType": "string or null",
                  "unitDisplayLabel": "string or null"
                },
                "primaryAggregation": "Sum" | "Average" | "Max" | "Min" | "Count" | "Latest",
                "baselineWindowDays": number (days before experiment for baseline, default 7),
                "runWindowDays": number (7-28, typically 14),
                "guardrailMetricDefinitionIds": ["guid-string", ...] (metrics to monitor for side effects)
              },
              "_summary": "Create 14-day experiment: Sleep by 10pm"
            }

            ### CheckInConsistencyNudge (ActionKind: ReflectPrompt)
            Prompt the user to reflect on their check-in consistency.
            targetEntityTitle: "Daily Check-ins"
            actionPayload: { "prompt": "string (a thoughtful reflection question)", "_summary": "Reflect on check-in patterns" }

            ### ExperimentEditSuggestion (ActionKind: Update)
            Suggest modifying an existing draft experiment.
            targetEntityTitle: Use the experiment's current title from the experiments list
            actionPayload: {
              "experimentId": "guid-string (required)",
              "newTitle": "string (optional)",
              "newDescription": "string (optional)",
              "_summary": "Update experiment description"
            }

            ### ExperimentArchiveSuggestion (ActionKind: Remove)
            Suggest abandoning an experiment that's no longer relevant.
            targetEntityTitle: Use the experiment's current title from the experiments list
            actionPayload: {
              "experimentId": "guid-string (required)",
              "reason": "string (optional)",
              "_summary": "Abandon stale experiment"
            }

            ## Field Reference
            """ + SchemaReference.ExperimentCategorySchema + """

            """ + SchemaReference.HypothesisGuidance + """

            """ + SchemaReference.MeasurementPlanGuidance + """

            """ + SchemaReference.MetricAggregationSchema + """

            """ + SchemaReference.MetricDataTypeSchema + """

            """ + SchemaReference.MetricDirectionSchema + """

            """ + SchemaReference.ExperimentFieldGuidance + """

            ## Multi-Entity Creation
            When creating an experiment that needs a NEW metric:
            1. Set primaryMetricDefinitionId to null
            2. Populate newPrimaryMetric with full metric definition
            3. The system will create the MetricDefinition first, then use it in the experiment

            ## Guardrail Metrics
            guardrailMetricDefinitionIds: Array of EXISTING metric IDs to monitor for side effects.
            Examples:
            - Sleep experiment: guardrail on "Energy Level" or "Focus"
            - Productivity experiment: guardrail on "Stress Level" or "Sleep Hours"
            - Fitness experiment: guardrail on "Recovery Score" or "Energy"
            Only use IDs from the Available Metrics list. Set to empty [] if no guardrails needed.

            ## Guidelines
            - Score MUST be 0.0-1.0 where 0.0=minimal impact, 1.0=maximum urgency/impact (e.g., 0.85 for high priority)
            - Experiments should be specific, time-bounded, and have a clear change + expected outcome
            - Frame experiments as curiosity, not obligation: "Let's find out if..." not "You should..."
            - Reflection prompts should be open-ended and non-judgmental
            - Connect experiments to observed patterns in the assessment
            - Only suggest ONE experiment per batch (the user shouldn't run multiple experiments simultaneously)
            - Experiment run window: 7-28 days (default 14)
            - Category should match the nature of the change
            - For Edit: only suggest for draft experiments, only include fields that need to change
            - For Archive: use when experiment is stale or no longer aligned with goals
            - ALWAYS include _summary in actionPayload - this is shown to the user before they accept
            - _summary should be concise (under 60 chars) and describe exactly what will happen

            ## Historical Context Usage
            If related experiment history is provided:
            - LEARN from completed experiments - incorporate their key findings
            - AVOID suggesting similar experiments to ones that failed or were inconclusive
            - Reference successful experiment outcomes when designing new hypotheses
            - Consider abandoned experiments as signals of what didn't work for this user
            - Build on the methodology of successful experiments
            """;
    }

    public static string BuildUserPrompt(
        SituationalAssessment assessment,
        IReadOnlyList<InterventionPlanItem> experimentInterventions,
        IReadOnlyList<ExperimentSnapshot> experiments,
        IReadOnlyList<MetricDefinitionSnapshot> metrics,
        IReadOnlyList<GoalSnapshot> goals,
        PreferencesSnapshot? preferences = null,
        SeasonSnapshot? season = null,
        RagContext? ragContext = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Assessment: {assessment.OverallMomentum} momentum, {assessment.EnergyTrend} energy");
        sb.AppendLine();

        // Season context for experiment intensity
        if (season is not null)
        {
            sb.AppendLine($"# Season Context: {season.Type} (intensity {season.Intensity}/10)");
            if (season.Type == "Recover")
                sb.AppendLine("NOTE: User is in recovery season — suggest gentler experiments with lower commitment");
            else if (season.Type == "Sprint")
                sb.AppendLine("NOTE: User is in sprint season — can accept more intensive experiments if they align with focus goals");
            sb.AppendLine();
        }

        // Coaching style affects experiment framing
        if (preferences is not null)
        {
            sb.AppendLine($"# Coaching Style: {preferences.CoachingStyle}");
            sb.AppendLine(preferences.CoachingStyle switch
            {
                "Direct" => "(Frame experiments concisely without excessive explanation)",
                "Analytical" => "(Include detailed reasoning and expected metrics in hypotheses)",
                _ => "(Frame experiments in an encouraging, supportive tone)"
            });
            sb.AppendLine();
        }

        if (assessment.Patterns.Count > 0)
        {
            sb.AppendLine("# Observed Patterns:");
            foreach (var p in assessment.Patterns)
                sb.AppendLine($"- {p}");
            sb.AppendLine();
        }

        if (assessment.KeyRisks.Count > 0)
        {
            sb.AppendLine("# Key Risks:");
            foreach (var r in assessment.KeyRisks)
                sb.AppendLine($"- [{r.Severity}] {r.Area}: {r.Detail}");
            sb.AppendLine();
        }

        sb.AppendLine("# Intervention Plan (your assignments):");
        foreach (var item in experimentInterventions)
        {
            sb.AppendLine($"- [{item.Priority}] {item.Area}: {item.Reasoning}");
            sb.AppendLine($"  Target type: {item.TargetType}");
        }
        sb.AppendLine();

        var activeExperiments = experiments.Where(e => e.Status == ExperimentStatus.Active).ToList();
        sb.AppendLine($"# Current Experiments: {activeExperiments.Count} active");
        foreach (var e in activeExperiments)
            sb.AppendLine($"- \"{e.Title}\" (running since {e.StartDate?.ToString("yyyy-MM-dd") ?? "?"})");

        if (activeExperiments.Count > 0)
            sb.AppendLine("NOTE: There is already an active experiment. Consider a reflection prompt instead of a new experiment.");

        sb.AppendLine();

        // Available goals for linking (include Draft for feedback)
        var activeGoals = goals.Where(g => g.Status == GoalStatus.Active || g.Status == GoalStatus.Draft).ToList();
        if (activeGoals.Count > 0)
        {
            sb.AppendLine($"# Available Goals for Linking ({activeGoals.Count})");
            sb.AppendLine("(Use linkedGoalIds to connect experiment to relevant goals)");
            foreach (var g in activeGoals)
                sb.AppendLine($"- [{g.Id}] \"{g.Title}\" | Status:{g.Status} | Priority:{g.Priority}");
            sb.AppendLine();
        }

        // Enhanced metrics section with full details
        sb.AppendLine($"# Available Metrics ({metrics.Count})");
        sb.AppendLine("Use for: primaryMetricDefinitionId (the main metric to track)");
        sb.AppendLine("         guardrailMetricDefinitionIds (side-effect monitors)");
        sb.AppendLine("Create new via newPrimaryMetric only if nothing appropriate exists.");
        sb.AppendLine();
        foreach (var m in metrics.Take(15))
        {
            sb.AppendLine($"- [{m.Id}] \"{m.Name}\"");
            sb.AppendLine($"  DataType: {m.DataType} | Direction: {m.Direction} | Unit: {m.UnitDisplayLabel ?? "none"}");
            sb.AppendLine($"  Default: {m.DefaultCadence} cadence, {m.DefaultAggregation} aggregation");
            var lastObs = m.LastObservationDate?.ToString("yyyy-MM-dd") ?? "never recorded";
            sb.AppendLine($"  Last observation: {lastObs}");
        }
        sb.AppendLine();

        // Suggest good guardrail candidates
        var guardrailCandidates = metrics
            .Where(m => m.Name.Contains("Sleep", StringComparison.OrdinalIgnoreCase) ||
                        m.Name.Contains("Energy", StringComparison.OrdinalIgnoreCase) ||
                        m.Name.Contains("Stress", StringComparison.OrdinalIgnoreCase) ||
                        m.Name.Contains("Focus", StringComparison.OrdinalIgnoreCase) ||
                        m.Direction == "Maintain")
            .Take(5)
            .ToList();
        if (guardrailCandidates.Count > 0)
        {
            sb.AppendLine("# Suggested Guardrail Metrics (for guardrailMetricDefinitionIds)");
            sb.AppendLine("These metrics are good candidates for monitoring side effects:");
            foreach (var m in guardrailCandidates)
                sb.AppendLine($"- [{m.Id}] \"{m.Name}\" ({m.Direction})");
            sb.AppendLine();
        }

        // Add critical constraints to prevent hallucinated IDs
        sb.AppendLine("## CRITICAL CONSTRAINTS");
        if (activeGoals.Count > 0)
            sb.AppendLine($"- VALID GOAL IDS for linkedGoalIds: {string.Join(", ", activeGoals.Select(g => g.Id))}");
        else
            sb.AppendLine("- NO GOALS EXIST. Set linkedGoalIds to empty array [].");

        if (metrics.Count > 0)
        {
            sb.AppendLine($"- VALID METRIC IDS for primaryMetricDefinitionId and guardrailMetricDefinitionIds:");
            sb.AppendLine($"  {string.Join(", ", metrics.Select(m => m.Id))}");
        }
        else
        {
            sb.AppendLine("- NO METRICS EXIST. You must use newPrimaryMetric to create one. Set guardrailMetricDefinitionIds to [].");
        }

        var allExperiments = experiments.Where(e => e.Status != ExperimentStatus.Abandoned).ToList();
        if (allExperiments.Count == 0)
        {
            sb.AppendLine("- NO EXPERIMENTS EXIST. Only generate ExperimentRecommendation (Create) or CheckInConsistencyNudge.");
            sb.AppendLine("- Do NOT generate ExperimentEditSuggestion or ExperimentArchiveSuggestion.");
        }
        else
        {
            sb.AppendLine("- For Update/Remove actions, you MUST use an experimentId from the experiments listed.");
            sb.AppendLine($"- VALID EXPERIMENT IDS: {string.Join(", ", allExperiments.Select(e => e.Id))}");
            sb.AppendLine("- Do NOT invent or hallucinate experiment IDs. Only use IDs that appear in the Current Experiments list.");
        }
        sb.AppendLine();

        // Add RAG historical context BEFORE generating - learn from past experiments
        RagContextFormatter.AppendForGeneration(sb, ragContext, "Experiment", DateOnly.FromDateTime(DateTime.UtcNow));

        sb.AppendLine("Generate recommendations for each intervention plan item assigned to you.");
        return sb.ToString();
    }
}

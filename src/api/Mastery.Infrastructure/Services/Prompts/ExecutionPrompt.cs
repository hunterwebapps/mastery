using Mastery.Domain.Entities.Recommendation;

namespace Mastery.Infrastructure.Services.Prompts;

/// <summary>
/// Prompt for LLM-based recommendation execution.
/// Instructs the model to parse ActionPayload and call the appropriate tool.
/// </summary>
internal static class ExecutionPrompt
{
    public const string PromptVersion = "exec-v1.0";
    public const string Model = "gpt-5-mini";

    public static string BuildSystemPrompt()
    {
        return """
            You are an execution agent for the Mastery personal development system.
            Your job is to execute a recommendation by calling the appropriate tool(s).

            ## Instructions
            1. Parse the recommendation's ActionPayload JSON
            2. Extract the required parameters for the tool call
            3. Call the appropriate tool based on the recommendation type and action kind
            4. For multi-step actions (like GoalScoreboardSuggestion with a new metric), you may need to call multiple tools

            ## Tool Selection Guidelines

            ### Task Actions
            - NextBestAction (ExecuteToday) → schedule_task_for_today
            - ScheduleAdjustmentSuggestion (Defer) → reschedule_task
            - PlanRealismAdjustment (Defer) → reschedule_task
            - TaskBreakdownSuggestion (Create) → create_task
            - TaskEditSuggestion (Update) → update_task
            - TaskArchiveSuggestion (Remove) → archive_task

            ### Habit Actions
            - HabitFromLeadMetricSuggestion (Create) → create_habit
            - HabitModeSuggestion (Update) → update_habit
            - HabitEditSuggestion (Update) → update_habit
            - HabitArchiveSuggestion (Remove) → archive_habit

            ### Experiment Actions
            - ExperimentRecommendation (Create) → create_experiment
            - ExperimentEditSuggestion (Update) → update_experiment
            - ExperimentArchiveSuggestion (Remove) → abandon_experiment

            ### Goal Actions
            - GoalScoreboardSuggestion (Update) → add_metric_to_goal (handles both existing and new metrics)
            - GoalEditSuggestion (Update) → update_goal
            - GoalArchiveSuggestion (Remove) → archive_goal

            ### Metric Actions
            - MetricObservationReminder (Create) → This is informational, no tool call needed
            - MetricEditSuggestion (Update) → update_metric_definition

            ### Project Actions
            - ProjectStuckFix (Create/Update) → If suggestedNewTask: create_task + set_project_next_action. If suggestedNextTaskId: set_project_next_action
            - ProjectSuggestion (Create) → create_project
            - ProjectEditSuggestion (Update) → update_project
            - ProjectArchiveSuggestion (Remove) → archive_project

            ## Important Notes
            - Always use the exact field values from the ActionPayload
            - For UUID fields, pass the string value directly (the tools accept strings)
            - If an optional field is null in the payload, don't include it in the tool call
            - For GoalScoreboardSuggestion: if existingMetricDefinitionId is provided, use it. Otherwise, use the newMetric* fields.
            """;
    }

    public static string BuildUserPrompt(Recommendation recommendation)
    {
        return $"""
            Execute this recommendation:

            Type: {recommendation.Type}
            ActionKind: {recommendation.ActionKind}
            TargetKind: {recommendation.Target.Kind}
            TargetEntityId: {recommendation.Target.EntityId?.ToString() ?? "null (new entity)"}

            ActionPayload:
            {recommendation.ActionPayload ?? "{}"}

            Call the appropriate tool(s) to execute this action.
            """;
    }
}

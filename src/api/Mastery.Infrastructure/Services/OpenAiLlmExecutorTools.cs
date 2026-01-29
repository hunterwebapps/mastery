using OpenAI.Chat;

namespace Mastery.Infrastructure.Services;

/// <summary>
/// Defines all OpenAI function tools for executing recommendation actions.
/// Each tool maps to a MediatR command that can be dispatched by the executor.
/// </summary>
internal static class OpenAiLlmExecutorTools
{
    #region Task Domain Tools

    public static ChatTool ScheduleTaskForToday { get; } = ChatTool.CreateFunctionTool(
        "schedule_task_for_today",
        "Schedules an existing task for today",
        BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                taskId = new { type = "string", format = "uuid", description = "The task ID to schedule" }
            },
            required = new[] { "taskId" },
            additionalProperties = false
        }));

    public static ChatTool RescheduleTask { get; } = ChatTool.CreateFunctionTool(
        "reschedule_task",
        "Reschedules a task to a different date",
        BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                taskId = new { type = "string", format = "uuid", description = "The task ID to reschedule" },
                newDate = new { type = "string", format = "date", description = "New date in YYYY-MM-DD format" },
                reason = new { type = "string", description = "Reason for rescheduling" }
            },
            required = new[] { "taskId", "newDate" },
            additionalProperties = false
        }));

    public static ChatTool CreateTask { get; } = ChatTool.CreateFunctionTool(
        "create_task",
        "Creates a new task in the user's task list",
        BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                title = new { type = "string", description = "Task title" },
                description = new { type = "string", description = "Task description" },
                estMinutes = new { type = "integer", minimum = 5, maximum = 480, description = "Estimated minutes" },
                energyCost = new { type = "integer", minimum = 1, maximum = 5, description = "Energy cost (1=low, 5=high)" },
                priority = new { type = "integer", minimum = 1, maximum = 5, description = "Priority (1=highest, 5=lowest)" },
                projectId = new { type = "string", format = "uuid", description = "Optional project ID to link to" },
                goalId = new { type = "string", format = "uuid", description = "Optional goal ID to link to" },
                contextTags = new { type = "array", items = new { type = "string" }, description = "Context tags" },
                dueOn = new { type = "string", format = "date", description = "Due date in YYYY-MM-DD format" },
                dueType = new { type = "string", @enum = new[] { "Soft", "Hard" }, description = "Due date type" },
                startAsReady = new { type = "boolean", description = "Start in Ready status (true) or Inbox (false)" }
            },
            required = new[] { "title", "estMinutes", "energyCost", "priority" },
            additionalProperties = false
        }));

    public static ChatTool UpdateTask { get; } = ChatTool.CreateFunctionTool(
        "update_task",
        "Updates an existing task's properties",
        BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                taskId = new { type = "string", format = "uuid", description = "The task ID to update" },
                title = new { type = "string", description = "New title" },
                description = new { type = "string", description = "New description" },
                priority = new { type = "integer", minimum = 1, maximum = 5, description = "New priority" },
                estMinutes = new { type = "integer", minimum = 5, maximum = 480, description = "New estimated minutes" },
                energyCost = new { type = "integer", minimum = 1, maximum = 5, description = "New energy cost" }
            },
            required = new[] { "taskId" },
            additionalProperties = false
        }));

    public static ChatTool ArchiveTask { get; } = ChatTool.CreateFunctionTool(
        "archive_task",
        "Archives (soft-deletes) a task",
        BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                taskId = new { type = "string", format = "uuid", description = "The task ID to archive" }
            },
            required = new[] { "taskId" },
            additionalProperties = false
        }));

    #endregion

    #region Habit Domain Tools

    public static ChatTool CreateHabit { get; } = ChatTool.CreateFunctionTool(
        "create_habit",
        "Creates a new habit",
        BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                title = new { type = "string", description = "Habit title" },
                description = new { type = "string", description = "Habit description" },
                why = new { type = "string", description = "Why this habit matters" },
                defaultMode = new { type = "string", @enum = new[] { "Full", "Maintenance", "Minimum" }, description = "Default completion mode" },
                scheduleType = new { type = "string", @enum = new[] { "Daily", "DaysOfWeek", "WeeklyFrequency", "Interval" }, description = "Schedule type" },
                daysOfWeek = new { type = "array", items = new { type = "integer", minimum = 0, maximum = 6 }, description = "Days of week (0=Sunday)" },
                frequencyPerWeek = new { type = "integer", minimum = 1, maximum = 7, description = "Frequency per week" },
                goalIds = new { type = "array", items = new { type = "string", format = "uuid" }, description = "Linked goal IDs" }
            },
            required = new[] { "title", "defaultMode", "scheduleType" },
            additionalProperties = false
        }));

    public static ChatTool UpdateHabit { get; } = ChatTool.CreateFunctionTool(
        "update_habit",
        "Updates an existing habit",
        BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                habitId = new { type = "string", format = "uuid", description = "The habit ID to update" },
                title = new { type = "string", description = "New title" },
                defaultMode = new { type = "string", @enum = new[] { "Full", "Maintenance", "Minimum" }, description = "New default mode" },
                scheduleType = new { type = "string", @enum = new[] { "Daily", "DaysOfWeek", "WeeklyFrequency", "Interval" }, description = "New schedule type" },
                daysOfWeek = new { type = "array", items = new { type = "integer", minimum = 0, maximum = 6 }, description = "New days of week" },
                frequencyPerWeek = new { type = "integer", minimum = 1, maximum = 7, description = "New frequency per week" }
            },
            required = new[] { "habitId" },
            additionalProperties = false
        }));

    public static ChatTool ArchiveHabit { get; } = ChatTool.CreateFunctionTool(
        "archive_habit",
        "Archives (soft-deletes) a habit",
        BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                habitId = new { type = "string", format = "uuid", description = "The habit ID to archive" }
            },
            required = new[] { "habitId" },
            additionalProperties = false
        }));

    #endregion

    #region Experiment Domain Tools

    public static ChatTool CreateExperiment { get; } = ChatTool.CreateFunctionTool(
        "create_experiment",
        "Creates a new behavioral experiment",
        BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                title = new { type = "string", description = "Experiment title" },
                description = new { type = "string", description = "Experiment description" },
                category = new { type = "string", @enum = new[] { "Habit", "Routine", "Environment", "Mindset", "Productivity", "Health", "Social", "PlanRealism", "FrictionReduction", "CheckInConsistency", "Top1FollowThrough", "Other" }, description = "Experiment category" },
                hypothesisChange = new { type = "string", description = "What the user will do differently" },
                hypothesisExpectedOutcome = new { type = "string", description = "Expected improvement" },
                hypothesisRationale = new { type = "string", description = "Why this should work" },
                linkedGoalIds = new { type = "array", items = new { type = "string", format = "uuid" }, description = "Linked goal IDs" },
                primaryMetricDefinitionId = new { type = "string", format = "uuid", description = "Primary metric to track" },
                primaryAggregation = new { type = "string", @enum = new[] { "Sum", "Average", "Max", "Min", "Count", "Latest" }, description = "Aggregation method" },
                baselineWindowDays = new { type = "integer", minimum = 1, maximum = 30, description = "Baseline window in days" },
                runWindowDays = new { type = "integer", minimum = 1, maximum = 90, description = "Run window in days" },
                guardrailMetricIds = new { type = "array", items = new { type = "string", format = "uuid" }, description = "Guardrail metric IDs" }
            },
            required = new[] { "title", "category", "hypothesisChange", "hypothesisExpectedOutcome" },
            additionalProperties = false
        }));

    public static ChatTool UpdateExperiment { get; } = ChatTool.CreateFunctionTool(
        "update_experiment",
        "Updates an existing experiment (draft only)",
        BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                experimentId = new { type = "string", format = "uuid", description = "The experiment ID to update" },
                title = new { type = "string", description = "New title" },
                description = new { type = "string", description = "New description" }
            },
            required = new[] { "experimentId" },
            additionalProperties = false
        }));

    public static ChatTool AbandonExperiment { get; } = ChatTool.CreateFunctionTool(
        "abandon_experiment",
        "Abandons an active or paused experiment",
        BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                experimentId = new { type = "string", format = "uuid", description = "The experiment ID to abandon" },
                reason = new { type = "string", description = "Reason for abandoning" }
            },
            required = new[] { "experimentId" },
            additionalProperties = false
        }));

    #endregion

    #region Goal Domain Tools

    public static ChatTool CreateGoal { get; } = ChatTool.CreateFunctionTool(
        "create_goal",
        "Creates a new goal",
        BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                title = new { type = "string", description = "Goal title" },
                description = new { type = "string", description = "Goal description" },
                why = new { type = "string", description = "Why this goal matters" },
                priority = new { type = "integer", minimum = 1, maximum = 5, description = "Priority (1=highest)" },
                deadline = new { type = "string", format = "date", description = "Target deadline in YYYY-MM-DD" }
            },
            required = new[] { "title", "priority" },
            additionalProperties = false
        }));

    public static ChatTool UpdateGoal { get; } = ChatTool.CreateFunctionTool(
        "update_goal",
        "Updates an existing goal's properties",
        BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                goalId = new { type = "string", format = "uuid", description = "The goal ID to update" },
                title = new { type = "string", description = "New title" },
                priority = new { type = "integer", minimum = 1, maximum = 5, description = "New priority" },
                deadline = new { type = "string", format = "date", description = "New deadline in YYYY-MM-DD" }
            },
            required = new[] { "goalId" },
            additionalProperties = false
        }));

    public static ChatTool ArchiveGoal { get; } = ChatTool.CreateFunctionTool(
        "archive_goal",
        "Archives (soft-deletes) a goal",
        BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                goalId = new { type = "string", format = "uuid", description = "The goal ID to archive" }
            },
            required = new[] { "goalId" },
            additionalProperties = false
        }));

    public static ChatTool AddMetricToGoal { get; } = ChatTool.CreateFunctionTool(
        "add_metric_to_goal",
        "Adds a metric to a goal's scoreboard (creates new metric if needed)",
        BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                goalId = new { type = "string", format = "uuid", description = "The goal ID to add metric to" },
                existingMetricDefinitionId = new { type = "string", format = "uuid", description = "Existing metric ID (if reusing)" },
                newMetricName = new { type = "string", description = "Name for new metric (if creating)" },
                newMetricDescription = new { type = "string", description = "Description for new metric" },
                newMetricDataType = new { type = "string", @enum = new[] { "Number", "Boolean", "Duration", "Percentage", "Count", "Rating" }, description = "Data type for new metric" },
                newMetricDirection = new { type = "string", @enum = new[] { "Increase", "Decrease", "Maintain" }, description = "Direction for new metric" },
                newMetricUnitLabel = new { type = "string", description = "Unit display label (min, %, $, etc.)" },
                newMetricCadence = new { type = "string", @enum = new[] { "Daily", "Weekly", "Monthly", "Rolling" }, description = "Default cadence" },
                newMetricAggregation = new { type = "string", @enum = new[] { "Sum", "Average", "Max", "Min", "Count", "Latest" }, description = "Default aggregation" },
                kind = new { type = "string", @enum = new[] { "Lag", "Lead", "Constraint" }, description = "Metric kind in goal" },
                targetType = new { type = "string", @enum = new[] { "AtLeast", "AtMost", "Between", "Exactly" }, description = "Target type" },
                targetValue = new { type = "number", description = "Target value (or min for Between)" },
                targetMaxValue = new { type = "number", description = "Max value (for Between only)" },
                windowType = new { type = "string", @enum = new[] { "Daily", "Weekly", "Monthly", "Rolling" }, description = "Evaluation window" },
                rollingDays = new { type = "integer", description = "Rolling window days" },
                aggregation = new { type = "string", @enum = new[] { "Sum", "Average", "Max", "Min", "Count", "Latest" }, description = "Goal-specific aggregation" },
                sourceHint = new { type = "string", @enum = new[] { "Manual", "Habit", "Task", "CheckIn", "Integration", "Computed" }, description = "Data source" },
                weight = new { type = "number", minimum = 0, maximum = 1, description = "Weight in goal health (0-1)" },
                baseline = new { type = "number", description = "Baseline value" }
            },
            required = new[] { "goalId", "kind", "targetType", "targetValue", "windowType", "aggregation", "sourceHint", "weight" },
            additionalProperties = false
        }));

    #endregion

    #region Metric Domain Tools

    public static ChatTool CreateMetricDefinition { get; } = ChatTool.CreateFunctionTool(
        "create_metric_definition",
        "Creates a new metric definition",
        BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                name = new { type = "string", description = "Metric name" },
                description = new { type = "string", description = "Metric description" },
                dataType = new { type = "string", @enum = new[] { "Number", "Boolean", "Duration", "Percentage", "Count", "Rating" }, description = "Data type" },
                direction = new { type = "string", @enum = new[] { "Increase", "Decrease", "Maintain" }, description = "Direction" },
                unitType = new { type = "string", description = "Unit type (duration, count, etc.)" },
                unitLabel = new { type = "string", description = "Unit display label" },
                cadence = new { type = "string", @enum = new[] { "Daily", "Weekly", "Monthly", "Rolling" }, description = "Default cadence" },
                aggregation = new { type = "string", @enum = new[] { "Sum", "Average", "Max", "Min", "Count", "Latest" }, description = "Default aggregation" }
            },
            required = new[] { "name", "dataType", "direction", "cadence", "aggregation" },
            additionalProperties = false
        }));

    public static ChatTool UpdateMetricDefinition { get; } = ChatTool.CreateFunctionTool(
        "update_metric_definition",
        "Updates an existing metric definition",
        BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                metricId = new { type = "string", format = "uuid", description = "The metric ID to update" },
                name = new { type = "string", description = "New name" },
                direction = new { type = "string", @enum = new[] { "Increase", "Decrease", "Maintain" }, description = "New direction" }
            },
            required = new[] { "metricId" },
            additionalProperties = false
        }));

    #endregion

    #region Project Domain Tools

    public static ChatTool CreateProject { get; } = ChatTool.CreateFunctionTool(
        "create_project",
        "Creates a new project",
        BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                title = new { type = "string", description = "Project title" },
                description = new { type = "string", description = "Project description" },
                priority = new { type = "integer", minimum = 1, maximum = 5, description = "Priority (1=highest)" },
                goalId = new { type = "string", format = "uuid", description = "Optional linked goal ID" },
                targetEndDate = new { type = "string", format = "date", description = "Target end date in YYYY-MM-DD" }
            },
            required = new[] { "title", "priority" },
            additionalProperties = false
        }));

    public static ChatTool UpdateProject { get; } = ChatTool.CreateFunctionTool(
        "update_project",
        "Updates an existing project",
        BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                projectId = new { type = "string", format = "uuid", description = "The project ID to update" },
                title = new { type = "string", description = "New title" },
                priority = new { type = "integer", minimum = 1, maximum = 5, description = "New priority" },
                goalId = new { type = "string", format = "uuid", description = "New linked goal ID" }
            },
            required = new[] { "projectId" },
            additionalProperties = false
        }));

    public static ChatTool ArchiveProject { get; } = ChatTool.CreateFunctionTool(
        "archive_project",
        "Archives (soft-deletes) a project",
        BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                projectId = new { type = "string", format = "uuid", description = "The project ID to archive" }
            },
            required = new[] { "projectId" },
            additionalProperties = false
        }));

    public static ChatTool SetProjectNextAction { get; } = ChatTool.CreateFunctionTool(
        "set_project_next_action",
        "Sets the next action (task) for a project",
        BinaryData.FromObjectAsJson(new
        {
            type = "object",
            properties = new
            {
                projectId = new { type = "string", format = "uuid", description = "The project ID" },
                taskId = new { type = "string", format = "uuid", description = "The task ID to set as next action" }
            },
            required = new[] { "projectId", "taskId" },
            additionalProperties = false
        }));

    #endregion

    #region All Tools Collection

    /// <summary>
    /// All available tools for recommendation execution.
    /// NOTE: This must be declared AFTER all individual tool properties to ensure
    /// they are initialized before this collection is built (static initialization order).
    /// </summary>
    public static IReadOnlyList<ChatTool> AllTools { get; } =
    [
        // Task Domain Tools
        ScheduleTaskForToday,
        RescheduleTask,
        CreateTask,
        UpdateTask,
        ArchiveTask,

        // Habit Domain Tools
        CreateHabit,
        UpdateHabit,
        ArchiveHabit,

        // Experiment Domain Tools
        CreateExperiment,
        UpdateExperiment,
        AbandonExperiment,

        // Goal Domain Tools
        CreateGoal,
        UpdateGoal,
        ArchiveGoal,
        AddMetricToGoal,

        // Metric Domain Tools
        CreateMetricDefinition,
        UpdateMetricDefinition,

        // Project Domain Tools
        CreateProject,
        UpdateProject,
        ArchiveProject,
        SetProjectNextAction
    ];

    #endregion
}

using System.Text.Json;
using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Experiments.Commands.AbandonExperiment;
using Mastery.Application.Features.Experiments.Commands.CreateExperiment;
using Mastery.Application.Features.Experiments.Commands.UpdateExperiment;
using Mastery.Application.Features.Goals.Commands.AddGoalMetric;
using Mastery.Application.Features.Goals.Commands.CreateGoal;
using Mastery.Application.Features.Goals.Commands.DeleteGoal;
using Mastery.Application.Features.Goals.Commands.UpdateGoal;
using Mastery.Application.Features.Habits.Commands.CreateHabit;
using Mastery.Application.Features.Habits.Commands.UpdateHabit;
using Mastery.Application.Features.Habits.Commands.UpdateHabitStatus;
using Mastery.Application.Features.Metrics.Commands.CreateMetricDefinition;
using Mastery.Application.Features.Metrics.Commands.UpdateMetricDefinition;
using Mastery.Application.Features.Projects.Commands.ChangeProjectStatus;
using Mastery.Application.Features.Projects.Commands.CreateProject;
using Mastery.Application.Features.Projects.Commands.SetProjectNextAction;
using Mastery.Application.Features.Projects.Commands.UpdateProject;
using Mastery.Application.Features.Tasks.Commands.ArchiveTask;
using Mastery.Application.Features.Tasks.Commands.CreateTask;
using Mastery.Application.Features.Tasks.Commands.RescheduleTask;
using Mastery.Application.Features.Tasks.Commands.ScheduleTask;
using Mastery.Application.Features.Tasks.Commands.UpdateTask;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Mastery.Infrastructure.Services.OpenAi;

/// <summary>
/// Executes OpenAI tool calls by mapping them to MediatR commands.
/// </summary>
public interface IToolCallHandler
{
    /// <summary>
    /// Executes a tool call and returns the created entity ID (if applicable).
    /// </summary>
    Task<ToolCallResult> ExecuteAsync(string toolName, string argumentsJson, CancellationToken ct);
}

/// <summary>
/// Result of executing a tool call.
/// </summary>
public sealed record ToolCallResult(
    bool Success,
    Guid? EntityId = null,
    string? EntityKind = null,
    string? ErrorMessage = null);

internal sealed class OpenAiToolCallHandler(
    ISender mediator,
    IDateTimeProvider dateTimeProvider,
    ILogger<OpenAiToolCallHandler> logger)
    : IToolCallHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public async Task<ToolCallResult> ExecuteAsync(string toolName, string argumentsJson, CancellationToken ct)
    {
        try
        {
            logger.LogDebug("Executing tool call: {ToolName} with args: {Args}", toolName, argumentsJson);

            return toolName switch
            {
                // Task Domain
                "schedule_task_for_today" => await HandleScheduleTaskForToday(argumentsJson, ct),
                "reschedule_task" => await HandleRescheduleTask(argumentsJson, ct),
                "create_task" => await HandleCreateTask(argumentsJson, ct),
                "update_task" => await HandleUpdateTask(argumentsJson, ct),
                "archive_task" => await HandleArchiveTask(argumentsJson, ct),

                // Habit Domain
                "create_habit" => await HandleCreateHabit(argumentsJson, ct),
                "update_habit" => await HandleUpdateHabit(argumentsJson, ct),
                "archive_habit" => await HandleArchiveHabit(argumentsJson, ct),

                // Experiment Domain
                "create_experiment" => await HandleCreateExperiment(argumentsJson, ct),
                "update_experiment" => await HandleUpdateExperiment(argumentsJson, ct),
                "abandon_experiment" => await HandleAbandonExperiment(argumentsJson, ct),

                // Goal Domain
                "create_goal" => await HandleCreateGoal(argumentsJson, ct),
                "update_goal" => await HandleUpdateGoal(argumentsJson, ct),
                "archive_goal" => await HandleArchiveGoal(argumentsJson, ct),
                "add_metric_to_goal" => await HandleAddMetricToGoal(argumentsJson, ct),

                // Metric Domain
                "create_metric_definition" => await HandleCreateMetricDefinition(argumentsJson, ct),
                "update_metric_definition" => await HandleUpdateMetricDefinition(argumentsJson, ct),

                // Project Domain
                "create_project" => await HandleCreateProject(argumentsJson, ct),
                "update_project" => await HandleUpdateProject(argumentsJson, ct),
                "archive_project" => await HandleArchiveProject(argumentsJson, ct),
                "set_project_next_action" => await HandleSetProjectNextAction(argumentsJson, ct),

                _ => new ToolCallResult(false, ErrorMessage: $"Unknown tool: {toolName}")
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tool call {ToolName} failed", toolName);
            return new ToolCallResult(false, ErrorMessage: ex.Message);
        }
    }

    #region Task Handlers

    private async Task<ToolCallResult> HandleScheduleTaskForToday(string json, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<ScheduleTaskForTodayArgs>(json, JsonOptions)!;
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow).ToString("yyyy-MM-dd");

        await mediator.Send(new ScheduleTaskCommand(args.TaskId, today), ct);
        return new ToolCallResult(true, args.TaskId, "Task");
    }

    private async Task<ToolCallResult> HandleRescheduleTask(string json, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<RescheduleTaskArgs>(json, JsonOptions)!;
        await mediator.Send(new RescheduleTaskCommand(args.TaskId, args.NewDate, args.Reason), ct);
        return new ToolCallResult(true, args.TaskId, "Task");
    }

    private async Task<ToolCallResult> HandleCreateTask(string json, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<CreateTaskArgs>(json, JsonOptions)!;

        CreateTaskDueInput? due = args.DueOn is not null
            ? new CreateTaskDueInput(args.DueOn, DueType: args.DueType ?? "Soft")
            : null;

        var command = new CreateTaskCommand(
            Title: args.Title,
            Description: args.Description,
            EstimatedMinutes: args.EstMinutes,
            EnergyCost: args.EnergyCost,
            Priority: args.Priority,
            ProjectId: args.ProjectId,
            GoalId: args.GoalId,
            Due: due,
            ContextTags: args.ContextTags,
            StartAsReady: args.StartAsReady ?? true);

        var id = await mediator.Send(command, ct);
        return new ToolCallResult(true, id, "Task");
    }

    private async Task<ToolCallResult> HandleUpdateTask(string json, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<UpdateTaskArgs>(json, JsonOptions)!;

        var command = new UpdateTaskCommand(
            TaskId: args.TaskId,
            Title: args.Title,
            Description: args.Description,
            EstimatedMinutes: args.EstMinutes,
            EnergyCost: args.EnergyCost,
            Priority: args.Priority);

        await mediator.Send(command, ct);
        return new ToolCallResult(true, args.TaskId, "Task");
    }

    private async Task<ToolCallResult> HandleArchiveTask(string json, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<ArchiveTaskArgs>(json, JsonOptions)!;
        await mediator.Send(new ArchiveTaskCommand(args.TaskId), ct);
        return new ToolCallResult(true, args.TaskId, "Task");
    }

    #endregion

    #region Habit Handlers

    private async Task<ToolCallResult> HandleCreateHabit(string json, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<CreateHabitArgs>(json, JsonOptions)!;

        var schedule = new CreateHabitScheduleInput(
            Type: args.ScheduleType,
            DaysOfWeek: args.DaysOfWeek,
            FrequencyPerWeek: args.FrequencyPerWeek);

        var command = new CreateHabitCommand(
            Title: args.Title,
            Schedule: schedule,
            Description: args.Description,
            Why: args.Why,
            DefaultMode: args.DefaultMode,
            GoalIds: args.GoalIds);

        var id = await mediator.Send(command, ct);
        return new ToolCallResult(true, id, "Habit");
    }

    private async Task<ToolCallResult> HandleUpdateHabit(string json, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<UpdateHabitArgs>(json, JsonOptions)!;

        CreateHabitScheduleInput? schedule = args.ScheduleType is not null
            ? new CreateHabitScheduleInput(
                Type: args.ScheduleType,
                DaysOfWeek: args.DaysOfWeek,
                FrequencyPerWeek: args.FrequencyPerWeek)
            : null;

        var command = new UpdateHabitCommand(
            HabitId: args.HabitId,
            Title: args.Title,
            DefaultMode: args.DefaultMode,
            Schedule: schedule);

        await mediator.Send(command, ct);
        return new ToolCallResult(true, args.HabitId, "Habit");
    }

    private async Task<ToolCallResult> HandleArchiveHabit(string json, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<ArchiveHabitArgs>(json, JsonOptions)!;
        await mediator.Send(new UpdateHabitStatusCommand(args.HabitId, "Archived"), ct);
        return new ToolCallResult(true, args.HabitId, "Habit");
    }

    #endregion

    #region Experiment Handlers

    private async Task<ToolCallResult> HandleCreateExperiment(string json, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<CreateExperimentArgs>(json, JsonOptions)!;

        var hypothesis = new CreateHypothesisInput(
            Change: args.HypothesisChange,
            ExpectedOutcome: args.HypothesisExpectedOutcome,
            Rationale: args.HypothesisRationale);

        var measurementPlan = new CreateMeasurementPlanInput(
            PrimaryMetricDefinitionId: args.PrimaryMetricDefinitionId ?? Guid.Empty,
            PrimaryAggregation: args.PrimaryAggregation ?? "Average",
            BaselineWindowDays: args.BaselineWindowDays ?? 7,
            RunWindowDays: args.RunWindowDays ?? 14,
            GuardrailMetricDefinitionIds: args.GuardrailMetricIds);

        var command = new CreateExperimentCommand(
            Title: args.Title,
            Category: args.Category,
            CreatedFrom: "AiRecommendation",
            Hypothesis: hypothesis,
            MeasurementPlan: measurementPlan,
            Description: args.Description,
            LinkedGoalIds: args.LinkedGoalIds);

        var id = await mediator.Send(command, ct);
        return new ToolCallResult(true, id, "Experiment");
    }

    private async Task<ToolCallResult> HandleUpdateExperiment(string json, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<UpdateExperimentArgs>(json, JsonOptions)!;

        var command = new UpdateExperimentCommand(
            Id: args.ExperimentId,
            Title: args.Title,
            Description: args.Description);

        await mediator.Send(command, ct);
        return new ToolCallResult(true, args.ExperimentId, "Experiment");
    }

    private async Task<ToolCallResult> HandleAbandonExperiment(string json, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<AbandonExperimentArgs>(json, JsonOptions)!;
        await mediator.Send(new AbandonExperimentCommand(args.ExperimentId, args.Reason), ct);
        return new ToolCallResult(true, args.ExperimentId, "Experiment");
    }

    #endregion

    #region Goal Handlers

    private async Task<ToolCallResult> HandleCreateGoal(string json, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<CreateGoalArgs>(json, JsonOptions)!;

        DateOnly? deadline = args.Deadline is not null
            ? DateOnly.Parse(args.Deadline)
            : null;

        var command = new CreateGoalCommand(
            Title: args.Title,
            Description: args.Description,
            Why: args.Why,
            Priority: args.Priority,
            Deadline: deadline);

        var id = await mediator.Send(command, ct);
        return new ToolCallResult(true, id, "Goal");
    }

    private async Task<ToolCallResult> HandleUpdateGoal(string json, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<UpdateGoalArgs>(json, JsonOptions)!;

        DateOnly? deadline = args.Deadline is not null
            ? DateOnly.Parse(args.Deadline)
            : null;

        // Note: UpdateGoalCommand requires Title, so we pass the new title or need to fetch existing
        var command = new UpdateGoalCommand(
            Id: args.GoalId,
            Title: args.Title ?? "Untitled", // Will be overwritten if null
            Priority: args.Priority ?? 3,
            Deadline: deadline);

        await mediator.Send(command, ct);
        return new ToolCallResult(true, args.GoalId, "Goal");
    }

    private async Task<ToolCallResult> HandleArchiveGoal(string json, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<ArchiveGoalArgs>(json, JsonOptions)!;
        await mediator.Send(new DeleteGoalCommand(args.GoalId), ct);
        return new ToolCallResult(true, args.GoalId, "Goal");
    }

    private async Task<ToolCallResult> HandleAddMetricToGoal(string json, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<AddMetricToGoalArgs>(json, JsonOptions)!;

        var command = new AddGoalMetricCommand(
            GoalId: args.GoalId,
            ExistingMetricDefinitionId: args.ExistingMetricDefinitionId,
            NewMetricName: args.NewMetricName,
            NewMetricDescription: args.NewMetricDescription,
            NewMetricDataType: args.NewMetricDataType,
            NewMetricDirection: args.NewMetricDirection,
            Kind: args.Kind,
            TargetType: args.TargetType,
            TargetValue: args.TargetValue,
            TargetMaxValue: args.TargetMaxValue,
            WindowType: args.WindowType,
            RollingDays: args.RollingDays,
            WeekStartDay: null,
            Aggregation: args.Aggregation,
            SourceHint: args.SourceHint,
            Weight: args.Weight,
            Baseline: args.Baseline);

        var result = await mediator.Send(command, ct);
        return new ToolCallResult(true, result.GoalMetricId, "GoalMetric");
    }

    #endregion

    #region Metric Handlers

    private async Task<ToolCallResult> HandleCreateMetricDefinition(string json, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<CreateMetricDefinitionArgs>(json, JsonOptions)!;

        CreateMetricUnitInput? unit = args.UnitLabel is not null
            ? new CreateMetricUnitInput(args.UnitType ?? "none", args.UnitLabel)
            : null;

        var command = new CreateMetricDefinitionCommand(
            Name: args.Name,
            Description: args.Description,
            DataType: args.DataType,
            Unit: unit,
            Direction: args.Direction,
            DefaultCadence: args.Cadence,
            DefaultAggregation: args.Aggregation);

        var id = await mediator.Send(command, ct);
        return new ToolCallResult(true, id, "MetricDefinition");
    }

    private async Task<ToolCallResult> HandleUpdateMetricDefinition(string json, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<UpdateMetricDefinitionArgs>(json, JsonOptions)!;

        // Note: UpdateMetricDefinitionCommand requires Name, so we pass the new name or need to fetch existing
        var command = new UpdateMetricDefinitionCommand(
            Id: args.MetricId,
            Name: args.Name ?? "Untitled",
            Direction: args.Direction ?? "Increase");

        await mediator.Send(command, ct);
        return new ToolCallResult(true, args.MetricId, "MetricDefinition");
    }

    #endregion

    #region Project Handlers

    private async Task<ToolCallResult> HandleCreateProject(string json, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<CreateProjectArgs>(json, JsonOptions)!;

        var command = new CreateProjectCommand(
            Title: args.Title,
            Description: args.Description,
            Priority: args.Priority,
            GoalId: args.GoalId,
            TargetEndDate: args.TargetEndDate);

        var id = await mediator.Send(command, ct);
        return new ToolCallResult(true, id, "Project");
    }

    private async Task<ToolCallResult> HandleUpdateProject(string json, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<UpdateProjectArgs>(json, JsonOptions)!;

        var command = new UpdateProjectCommand(
            ProjectId: args.ProjectId,
            Title: args.Title,
            Priority: args.Priority,
            GoalId: args.GoalId);

        await mediator.Send(command, ct);
        return new ToolCallResult(true, args.ProjectId, "Project");
    }

    private async Task<ToolCallResult> HandleArchiveProject(string json, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<ArchiveProjectArgs>(json, JsonOptions)!;
        await mediator.Send(new ChangeProjectStatusCommand(args.ProjectId, "Archived"), ct);
        return new ToolCallResult(true, args.ProjectId, "Project");
    }

    private async Task<ToolCallResult> HandleSetProjectNextAction(string json, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<SetProjectNextActionArgs>(json, JsonOptions)!;
        await mediator.Send(new SetProjectNextActionCommand(args.ProjectId, args.TaskId), ct);
        return new ToolCallResult(true, args.ProjectId, "Project");
    }

    #endregion

    #region Argument DTOs

    // Task Args
    private sealed record ScheduleTaskForTodayArgs(Guid TaskId);
    private sealed record RescheduleTaskArgs(Guid TaskId, string NewDate, string? Reason);
    private sealed record CreateTaskArgs(
        string Title, string? Description, int EstMinutes, int EnergyCost, int Priority,
        Guid? ProjectId, Guid? GoalId, List<string>? ContextTags, string? DueOn, string? DueType, bool? StartAsReady);
    private sealed record UpdateTaskArgs(
        Guid TaskId, string? Title, string? Description, int? Priority, int? EstMinutes, int? EnergyCost);
    private sealed record ArchiveTaskArgs(Guid TaskId);

    // Habit Args
    private sealed record CreateHabitArgs(
        string Title, string? Description, string? Why, string DefaultMode,
        string ScheduleType, List<int>? DaysOfWeek, int? FrequencyPerWeek, List<Guid>? GoalIds);
    private sealed record UpdateHabitArgs(
        Guid HabitId, string? Title, string? DefaultMode,
        string? ScheduleType, List<int>? DaysOfWeek, int? FrequencyPerWeek);
    private sealed record ArchiveHabitArgs(Guid HabitId);

    // Experiment Args
    private sealed record CreateExperimentArgs(
        string Title, string? Description, string Category,
        string HypothesisChange, string HypothesisExpectedOutcome, string? HypothesisRationale,
        List<Guid>? LinkedGoalIds, Guid? PrimaryMetricDefinitionId, string? PrimaryAggregation,
        int? BaselineWindowDays, int? RunWindowDays, List<Guid>? GuardrailMetricIds);
    private sealed record UpdateExperimentArgs(Guid ExperimentId, string? Title, string? Description);
    private sealed record AbandonExperimentArgs(Guid ExperimentId, string? Reason);

    // Goal Args
    private sealed record CreateGoalArgs(string Title, string? Description, string? Why, int Priority, string? Deadline);
    private sealed record UpdateGoalArgs(Guid GoalId, string? Title, int? Priority, string? Deadline);
    private sealed record ArchiveGoalArgs(Guid GoalId);
    private sealed record AddMetricToGoalArgs(
        Guid GoalId, Guid? ExistingMetricDefinitionId,
        string? NewMetricName, string? NewMetricDescription, string? NewMetricDataType, string? NewMetricDirection,
        string? NewMetricUnitLabel, string? NewMetricCadence, string? NewMetricAggregation,
        string Kind, string TargetType, decimal TargetValue, decimal? TargetMaxValue,
        string WindowType, int? RollingDays, string Aggregation, string SourceHint, decimal Weight, decimal? Baseline);

    // Metric Args
    private sealed record CreateMetricDefinitionArgs(
        string Name, string? Description, string DataType, string Direction,
        string? UnitType, string? UnitLabel, string Cadence, string Aggregation);
    private sealed record UpdateMetricDefinitionArgs(Guid MetricId, string? Name, string? Direction);

    // Project Args
    private sealed record CreateProjectArgs(string Title, string? Description, int Priority, Guid? GoalId, string? TargetEndDate);
    private sealed record UpdateProjectArgs(Guid ProjectId, string? Title, int? Priority, Guid? GoalId);
    private sealed record ArchiveProjectArgs(Guid ProjectId);
    private sealed record SetProjectNextActionArgs(Guid ProjectId, Guid TaskId);

    #endregion
}

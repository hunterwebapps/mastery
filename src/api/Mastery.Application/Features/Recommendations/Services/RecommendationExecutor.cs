using System.Text.Json;
using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Experiments.Commands.CreateExperiment;
using Mastery.Application.Features.Goals.Commands.CreateGoal;
using Mastery.Application.Features.Habits.Commands.CreateHabit;
using Mastery.Application.Features.Habits.Commands.UpdateHabit;
using Mastery.Application.Features.Metrics.Commands.CreateMetricDefinition;
using Mastery.Application.Features.Projects.Commands.CreateProject;
using Mastery.Application.Features.Tasks.Commands.CreateTask;
using Mastery.Application.Features.Tasks.Commands.RescheduleTask;
using Mastery.Application.Features.Tasks.Commands.ScheduleTask;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Mastery.Application.Features.Recommendations.Services;

/// <summary>
/// Dispatches the appropriate MediatR command when a recommendation is accepted.
/// Reads ActionKind, Target.Kind, and ActionPayload to determine what to do.
/// </summary>
public sealed class RecommendationExecutor : IRecommendationExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly ISender _mediator;
    private readonly ILogger<RecommendationExecutor> _logger;

    public RecommendationExecutor(ISender mediator, ILogger<RecommendationExecutor> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Guid?> ExecuteAsync(Recommendation recommendation, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(recommendation.ActionPayload))
        {
            _logger.LogDebug(
                "Recommendation {Id} has no ActionPayload, skipping execution",
                recommendation.Id);
            return null;
        }

        try
        {
            return (recommendation.ActionKind, recommendation.Target.Kind) switch
            {
                (RecommendationActionKind.Create, RecommendationTargetKind.Task) =>
                    await ExecuteCreateTask(recommendation.ActionPayload, cancellationToken),

                (RecommendationActionKind.Create, RecommendationTargetKind.Habit) =>
                    await ExecuteCreateHabit(recommendation.ActionPayload, cancellationToken),

                (RecommendationActionKind.Create, RecommendationTargetKind.Experiment) =>
                    await ExecuteCreateExperiment(recommendation.ActionPayload, cancellationToken),

                (RecommendationActionKind.Create, RecommendationTargetKind.Goal) =>
                    await ExecuteCreateGoal(recommendation.ActionPayload, cancellationToken),

                (RecommendationActionKind.Create, RecommendationTargetKind.Metric) =>
                    await ExecuteCreateMetric(recommendation.ActionPayload, cancellationToken),

                (RecommendationActionKind.Create, RecommendationTargetKind.Project) =>
                    await ExecuteCreateProject(recommendation.ActionPayload, cancellationToken),

                (RecommendationActionKind.Update, RecommendationTargetKind.Habit) =>
                    await ExecuteUpdateHabit(recommendation.ActionPayload, cancellationToken),

                (RecommendationActionKind.ExecuteToday, RecommendationTargetKind.Task) =>
                    await ExecuteScheduleTaskToday(recommendation.ActionPayload, cancellationToken),

                (RecommendationActionKind.Defer, RecommendationTargetKind.Task) =>
                    await ExecuteRescheduleTask(recommendation.ActionPayload, cancellationToken),

                (RecommendationActionKind.ReflectPrompt, _) => null,
                (RecommendationActionKind.LearnPrompt, _) => null,

                _ => LogUnsupported(recommendation)
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex,
                "Failed to deserialize ActionPayload for recommendation {Id} ({ActionKind}/{TargetKind})",
                recommendation.Id, recommendation.ActionKind, recommendation.Target.Kind);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to execute recommendation {Id} ({ActionKind}/{TargetKind})",
                recommendation.Id, recommendation.ActionKind, recommendation.Target.Kind);
            return null;
        }
    }

    private async Task<Guid> ExecuteCreateTask(string payload, CancellationToken ct)
    {
        var p = JsonSerializer.Deserialize<CreateTaskPayload>(payload, JsonOptions)!;
        var command = new CreateTaskCommand(
            Title: p.Title,
            Description: p.Description,
            EstimatedMinutes: p.EstMinutes ?? 30,
            EnergyCost: p.EnergyCost ?? 3,
            Priority: p.Priority ?? 3,
            ProjectId: p.ProjectId,
            GoalId: p.GoalId,
            ContextTags: p.ContextTags,
            StartAsReady: p.StartAsReady ?? true);

        return await _mediator.Send(command, ct);
    }

    private async Task<Guid> ExecuteCreateHabit(string payload, CancellationToken ct)
    {
        var p = JsonSerializer.Deserialize<CreateHabitPayload>(payload, JsonOptions)!;
        var schedule = new CreateHabitScheduleInput(
            Type: p.Schedule?.Type ?? "Daily",
            DaysOfWeek: p.Schedule?.DaysOfWeek,
            FrequencyPerWeek: p.Schedule?.FrequencyPerWeek);

        var command = new CreateHabitCommand(
            Title: p.Title,
            Schedule: schedule,
            Description: p.Description,
            Why: p.Why,
            DefaultMode: p.DefaultMode ?? "Full");

        return await _mediator.Send(command, ct);
    }

    private async Task<Guid> ExecuteCreateExperiment(string payload, CancellationToken ct)
    {
        var p = JsonSerializer.Deserialize<CreateExperimentPayload>(payload, JsonOptions)!;
        var hypothesis = new CreateHypothesisInput(
            Change: p.Hypothesis?.Change ?? p.Title,
            ExpectedOutcome: p.Hypothesis?.ExpectedOutcome ?? "Improvement expected",
            Rationale: p.Hypothesis?.Rationale);

        var measurementPlan = new CreateMeasurementPlanInput(
            PrimaryMetricDefinitionId: p.MeasurementPlan?.PrimaryMetricDefinitionId ?? Guid.Empty,
            PrimaryAggregation: p.MeasurementPlan?.PrimaryAggregation ?? "Average",
            RunWindowDays: p.MeasurementPlan?.RunWindowDays ?? 14);

        var command = new CreateExperimentCommand(
            Title: p.Title,
            Category: p.Category ?? "Behavioral",
            CreatedFrom: "AiRecommendation",
            Hypothesis: hypothesis,
            MeasurementPlan: measurementPlan,
            Description: p.Description,
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow));

        return await _mediator.Send(command, ct);
    }

    private async Task<Guid> ExecuteCreateGoal(string payload, CancellationToken ct)
    {
        var p = JsonSerializer.Deserialize<CreateGoalPayload>(payload, JsonOptions)!;
        var command = new CreateGoalCommand(
            Title: p.Title,
            Description: p.Description,
            Why: p.Why,
            Priority: p.Priority ?? 3,
            Deadline: p.Deadline);

        return await _mediator.Send(command, ct);
    }

    private async Task<Guid> ExecuteCreateMetric(string payload, CancellationToken ct)
    {
        var p = JsonSerializer.Deserialize<CreateMetricPayload>(payload, JsonOptions)!;
        var command = new CreateMetricDefinitionCommand(
            Name: p.Name,
            Description: p.Description,
            DataType: p.DataType ?? "Number",
            Direction: p.Direction ?? "Increase",
            DefaultCadence: p.DefaultCadence ?? "Daily",
            DefaultAggregation: p.DefaultAggregation ?? "Sum");

        return await _mediator.Send(command, ct);
    }

    private async Task<Guid> ExecuteCreateProject(string payload, CancellationToken ct)
    {
        var p = JsonSerializer.Deserialize<CreateProjectPayload>(payload, JsonOptions)!;
        var command = new CreateProjectCommand(
            Title: p.Title,
            Description: p.Description,
            Priority: p.Priority ?? 3,
            GoalId: p.GoalId);

        return await _mediator.Send(command, ct);
    }

    private async Task<Guid?> ExecuteUpdateHabit(string payload, CancellationToken ct)
    {
        var p = JsonSerializer.Deserialize<UpdateHabitPayload>(payload, JsonOptions)!;
        if (p.HabitId == Guid.Empty) return null;

        var command = new UpdateHabitCommand(
            HabitId: p.HabitId,
            DefaultMode: p.DefaultMode);

        await _mediator.Send(command, ct);
        return p.HabitId;
    }

    private async Task<Guid?> ExecuteScheduleTaskToday(string payload, CancellationToken ct)
    {
        var p = JsonSerializer.Deserialize<ScheduleTaskPayload>(payload, JsonOptions)!;
        if (p.TaskId == Guid.Empty) return null;

        var command = new ScheduleTaskCommand(
            TaskId: p.TaskId,
            ScheduledOn: DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"));

        await _mediator.Send(command, ct);
        return p.TaskId;
    }

    private async Task<Guid?> ExecuteRescheduleTask(string payload, CancellationToken ct)
    {
        var p = JsonSerializer.Deserialize<RescheduleTaskPayload>(payload, JsonOptions)!;
        if (p.TaskId == Guid.Empty) return null;

        var command = new RescheduleTaskCommand(
            TaskId: p.TaskId,
            NewDate: p.NewDate,
            Reason: p.Reason);

        await _mediator.Send(command, ct);
        return p.TaskId;
    }

    private Guid? LogUnsupported(Recommendation recommendation)
    {
        _logger.LogWarning(
            "Unsupported ActionKind/TargetKind combination: {ActionKind}/{TargetKind} for recommendation {Id}",
            recommendation.ActionKind, recommendation.Target.Kind, recommendation.Id);
        return null;
    }

    // Payload DTOs for deserialization
    private sealed record CreateTaskPayload(
        string Title, string? Description = null, int? EstMinutes = null,
        int? EnergyCost = null, int? Priority = null, Guid? ProjectId = null,
        Guid? GoalId = null, List<string>? ContextTags = null, bool? StartAsReady = null);

    private sealed record CreateHabitPayload(
        string Title, string? Description = null, string? Why = null,
        string? DefaultMode = null, HabitSchedulePayload? Schedule = null);

    private sealed record HabitSchedulePayload(
        string Type, List<int>? DaysOfWeek = null, int? FrequencyPerWeek = null);

    private sealed record CreateExperimentPayload(
        string Title, string? Description = null, string? Category = null,
        ExperimentHypothesisPayload? Hypothesis = null,
        ExperimentMeasurementPlanPayload? MeasurementPlan = null);

    private sealed record ExperimentHypothesisPayload(
        string Change, string ExpectedOutcome, string? Rationale = null);

    private sealed record ExperimentMeasurementPlanPayload(
        Guid PrimaryMetricDefinitionId, string? PrimaryAggregation = null,
        int? RunWindowDays = null);

    private sealed record CreateGoalPayload(
        string Title, string? Description = null, string? Why = null,
        int? Priority = null, DateOnly? Deadline = null);

    private sealed record CreateMetricPayload(
        string Name, string? Description = null, string? DataType = null,
        string? Direction = null, string? DefaultCadence = null,
        string? DefaultAggregation = null);

    private sealed record CreateProjectPayload(
        string Title, string? Description = null, int? Priority = null,
        Guid? GoalId = null);

    private sealed record UpdateHabitPayload(Guid HabitId, string? DefaultMode = null);

    private sealed record ScheduleTaskPayload(Guid TaskId);

    private sealed record RescheduleTaskPayload(Guid TaskId, string NewDate, string? Reason = null);
}

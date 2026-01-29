using System.Text.Json;
using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Recommendations.Models;
using Mastery.Application.Features.Tasks.Commands.RescheduleTask;
using Mastery.Application.Features.Tasks.Commands.ScheduleTask;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Mastery.Application.Features.Recommendations.Services;

/// <summary>
/// Handles server-side execution of simple recommendation actions (ExecuteToday, Defer).
/// These actions don't require user form input and can be executed directly.
/// </summary>
public interface ISimpleActionExecutor
{
    /// <summary>
    /// Returns true if this executor can handle the given action kind.
    /// </summary>
    bool CanExecute(RecommendationActionKind actionKind);

    /// <summary>
    /// Executes the recommendation action server-side.
    /// </summary>
    Task<ExecutionResult> ExecuteAsync(Recommendation recommendation, CancellationToken ct);
}

public sealed class SimpleActionExecutor(
    ISender mediator,
    IDateTimeProvider dateTimeProvider,
    ILogger<SimpleActionExecutor> logger)
    : ISimpleActionExecutor
{
    private static readonly HashSet<RecommendationActionKind> ServerSideActions =
    [
        RecommendationActionKind.ExecuteToday,
        RecommendationActionKind.Defer
    ];

    public bool CanExecute(RecommendationActionKind actionKind) =>
        ServerSideActions.Contains(actionKind);

    public async Task<ExecutionResult> ExecuteAsync(Recommendation recommendation, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(recommendation.ActionPayload))
        {
            return ExecutionResult.Failed("No action payload provided.");
        }

        try
        {
            return recommendation.ActionKind switch
            {
                RecommendationActionKind.ExecuteToday => await ExecuteTodayAsync(recommendation, ct),
                RecommendationActionKind.Defer => await DeferAsync(recommendation, ct),
                _ => ExecutionResult.Failed($"Action kind {recommendation.ActionKind} is not supported for server-side execution.")
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute {ActionKind} for recommendation {Id}",
                recommendation.ActionKind, recommendation.Id);
            return ExecutionResult.Failed(ex.Message);
        }
    }

    private async Task<ExecutionResult> ExecuteTodayAsync(Recommendation recommendation, CancellationToken ct)
    {
        var taskId = ExtractTaskId(recommendation);
        if (taskId is null)
        {
            return ExecutionResult.Failed("Could not extract task ID from recommendation.");
        }

        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);
        await mediator.Send(new ScheduleTaskCommand(taskId.Value, today.ToString("yyyy-MM-dd")), ct);

        logger.LogInformation("Scheduled task {TaskId} for today via recommendation {RecId}",
            taskId, recommendation.Id);

        return ExecutionResult.ForServerExecuted(taskId.Value, "Task");
    }

    private async Task<ExecutionResult> DeferAsync(Recommendation recommendation, CancellationToken ct)
    {
        var taskId = ExtractTaskId(recommendation);
        if (taskId is null)
        {
            return ExecutionResult.Failed("Could not extract task ID from recommendation.");
        }

        var newDate = ExtractNewDate(recommendation);
        if (string.IsNullOrEmpty(newDate))
        {
            return ExecutionResult.Failed("Could not extract new date from recommendation payload.");
        }

        var reason = ExtractReason(recommendation);
        await mediator.Send(new RescheduleTaskCommand(taskId.Value, newDate, reason), ct);

        logger.LogInformation("Rescheduled task {TaskId} to {NewDate} via recommendation {RecId}",
            taskId, newDate, recommendation.Id);

        return ExecutionResult.ForServerExecuted(taskId.Value, "Task");
    }

    private static Guid? ExtractTaskId(Recommendation recommendation)
    {
        // First try the target entity ID
        if (recommendation.Target.EntityId.HasValue)
        {
            return recommendation.Target.EntityId;
        }

        // Then try to extract from action payload
        try
        {
            using var doc = JsonDocument.Parse(recommendation.ActionPayload!);
            if (doc.RootElement.TryGetProperty("taskId", out var taskIdProp) &&
                taskIdProp.ValueKind == JsonValueKind.String &&
                Guid.TryParse(taskIdProp.GetString(), out var taskId))
            {
                return taskId;
            }
        }
        catch
        {
            // Ignore parse errors
        }

        return null;
    }

    private static string? ExtractNewDate(Recommendation recommendation)
    {
        try
        {
            using var doc = JsonDocument.Parse(recommendation.ActionPayload!);
            if (doc.RootElement.TryGetProperty("newDate", out var prop) &&
                prop.ValueKind == JsonValueKind.String)
            {
                return prop.GetString();
            }
            if (doc.RootElement.TryGetProperty("scheduledOn", out var prop2) &&
                prop2.ValueKind == JsonValueKind.String)
            {
                return prop2.GetString();
            }
        }
        catch
        {
            // Ignore parse errors
        }

        return null;
    }

    private static string? ExtractReason(Recommendation recommendation)
    {
        try
        {
            using var doc = JsonDocument.Parse(recommendation.ActionPayload!);
            if (doc.RootElement.TryGetProperty("reason", out var prop) &&
                prop.ValueKind == JsonValueKind.String)
            {
                return prop.GetString();
            }
        }
        catch
        {
            // Ignore parse errors
        }

        return null;
    }
}

using MediatR;
using Mastery.Application.Features.Learning.Services;
using Mastery.Domain.Entities.Task;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mastery.Application.Features.Learning.EventHandlers;

/// <summary>
/// Updates intervention outcomes when a task is completed.
/// This closes the feedback loop for recommendations that targeted this task.
/// </summary>
public sealed class TaskCompletionFeedbackHandler(
    IRecommendationRepository _recommendationRepository,
    ILearningEngineService _learningEngine,
    ILogger<TaskCompletionFeedbackHandler> _logger)
    : INotificationHandler<TaskCompletedEvent>
{
    // Only look for recommendations from the last 7 days
    private static readonly TimeSpan LookbackWindow = TimeSpan.FromDays(7);

    public async System.Threading.Tasks.Task Handle(TaskCompletedEvent notification, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow - LookbackWindow;

        // Find accepted recommendations that targeted this task
        var recommendations = await _recommendationRepository.GetAcceptedForTargetAsync(
            notification.UserId,
            RecommendationTargetKind.Task,
            notification.TaskId,
            acceptedAfter: cutoff,
            cancellationToken);

        if (recommendations.Count == 0)
        {
            _logger.LogDebug(
                "No accepted recommendations found for completed task {TaskId}",
                notification.TaskId);
            return;
        }

        _logger.LogInformation(
            "Updating {Count} recommendation outcome(s) for completed task {TaskId}",
            recommendations.Count, notification.TaskId);

        foreach (var recommendation in recommendations)
        {
            await _learningEngine.RecordActualCompletionAsync(
                recommendation.Id,
                wasCompleted: true,
                cancellationToken);
        }
    }
}

/// <summary>
/// Handles task completion undo by updating intervention outcomes.
/// </summary>
public sealed class TaskCompletionUndoFeedbackHandler(
    IRecommendationRepository _recommendationRepository,
    ILearningEngineService _learningEngine,
    ILogger<TaskCompletionUndoFeedbackHandler> _logger)
    : INotificationHandler<TaskCompletionUndoneEvent>
{
    private static readonly TimeSpan LookbackWindow = TimeSpan.FromDays(7);

    public async System.Threading.Tasks.Task Handle(TaskCompletionUndoneEvent notification, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow - LookbackWindow;

        var recommendations = await _recommendationRepository.GetAcceptedForTargetAsync(
            notification.UserId,
            RecommendationTargetKind.Task,
            notification.TaskId,
            acceptedAfter: cutoff,
            cancellationToken);

        if (recommendations.Count == 0)
            return;

        _logger.LogInformation(
            "Updating {Count} recommendation outcome(s) for undone task {TaskId}",
            recommendations.Count, notification.TaskId);

        foreach (var recommendation in recommendations)
        {
            await _learningEngine.RecordActualCompletionAsync(
                recommendation.Id,
                wasCompleted: false,
                cancellationToken);
        }
    }
}

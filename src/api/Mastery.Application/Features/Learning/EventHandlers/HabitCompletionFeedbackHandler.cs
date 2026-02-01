using MediatR;
using Mastery.Application.Features.Learning.Services;
using Mastery.Domain.Entities.Habit;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mastery.Application.Features.Learning.EventHandlers;

/// <summary>
/// Updates intervention outcomes when a habit is completed.
/// This closes the feedback loop for recommendations that targeted this habit or occurrence.
/// </summary>
public sealed class HabitCompletionFeedbackHandler(
    IRecommendationRepository _recommendationRepository,
    ILearningEngineService _learningEngine,
    IUnitOfWork _unitOfWork,
    ILogger<HabitCompletionFeedbackHandler> _logger)
    : INotificationHandler<HabitCompletedEvent>
{
    private static readonly TimeSpan LookbackWindow = TimeSpan.FromDays(7);

    public async Task Handle(HabitCompletedEvent notification, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow - LookbackWindow;

        // Check for recommendations targeting the specific occurrence
        var occurrenceRecs = await _recommendationRepository.GetAcceptedForTargetAsync(
            notification.UserId,
            RecommendationTargetKind.HabitOccurrence,
            notification.OccurrenceId,
            acceptedAfter: cutoff,
            cancellationToken);

        // Also check for recommendations targeting the habit itself
        var habitRecs = await _recommendationRepository.GetAcceptedForTargetAsync(
            notification.UserId,
            RecommendationTargetKind.Habit,
            notification.HabitId,
            acceptedAfter: cutoff,
            cancellationToken);

        var allRecs = occurrenceRecs.Concat(habitRecs).ToList();

        if (allRecs.Count == 0)
        {
            _logger.LogDebug(
                "No accepted recommendations found for completed habit occurrence {OccurrenceId}",
                notification.OccurrenceId);
            return;
        }

        _logger.LogInformation(
            "Updating {Count} recommendation outcome(s) for completed habit {HabitId}",
            allRecs.Count, notification.HabitId);

        foreach (var recommendation in allRecs)
        {
            await _learningEngine.RecordActualCompletionAsync(
                recommendation.Id,
                wasCompleted: true,
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Handles habit undo by updating intervention outcomes.
/// </summary>
public sealed class HabitUndoFeedbackHandler(
    IRecommendationRepository _recommendationRepository,
    IHabitRepository _habitRepository,
    ILearningEngineService _learningEngine,
    IUnitOfWork _unitOfWork,
    ILogger<HabitUndoFeedbackHandler> _logger)
    : INotificationHandler<HabitUndoneEvent>
{
    private static readonly TimeSpan LookbackWindow = TimeSpan.FromDays(7);

    public async Task Handle(HabitUndoneEvent notification, CancellationToken cancellationToken)
    {
        // Get the habit to find the user ID
        var habit = await _habitRepository.GetByIdAsync(notification.HabitId, cancellationToken);
        if (habit == null)
        {
            _logger.LogWarning("Habit {HabitId} not found for undo feedback", notification.HabitId);
            return;
        }

        var cutoff = DateTime.UtcNow - LookbackWindow;

        var occurrenceRecs = await _recommendationRepository.GetAcceptedForTargetAsync(
            habit.UserId,
            RecommendationTargetKind.HabitOccurrence,
            notification.OccurrenceId,
            acceptedAfter: cutoff,
            cancellationToken);

        var habitRecs = await _recommendationRepository.GetAcceptedForTargetAsync(
            habit.UserId,
            RecommendationTargetKind.Habit,
            notification.HabitId,
            acceptedAfter: cutoff,
            cancellationToken);

        var allRecs = occurrenceRecs.Concat(habitRecs).ToList();

        if (allRecs.Count == 0)
            return;

        _logger.LogInformation(
            "Updating {Count} recommendation outcome(s) for undone habit {HabitId}",
            allRecs.Count, notification.HabitId);

        foreach (var recommendation in allRecs)
        {
            await _learningEngine.RecordActualCompletionAsync(
                recommendation.Id,
                wasCompleted: false,
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

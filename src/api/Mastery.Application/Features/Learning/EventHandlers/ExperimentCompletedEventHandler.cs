using MediatR;
using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Experiment;
using Mastery.Domain.Entities.Learning;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mastery.Application.Features.Learning.EventHandlers;

/// <summary>
/// Updates the user's playbook when an experiment is completed.
/// Experiment outcomes provide high-confidence signals for learning what works.
/// </summary>
public sealed class ExperimentCompletedEventHandler(
    IExperimentRepository _experimentRepository,
    IUserPlaybookRepository _playbookRepository,
    IDateTimeProvider _dateTimeProvider,
    ILogger<ExperimentCompletedEventHandler> _logger)
    : INotificationHandler<ExperimentCompletedEvent>
{
    // Higher learning rate for explicit experiments (they're deliberate tests)
    private const decimal ExperimentLearningRateMultiplier = 2.0m;

    public async Task Handle(ExperimentCompletedEvent notification, CancellationToken cancellationToken)
    {
        var experiment = await _experimentRepository.GetByIdAsync(notification.ExperimentId, cancellationToken);
        if (experiment == null)
        {
            _logger.LogWarning(
                "Experiment {ExperimentId} not found for completion event",
                notification.ExperimentId);
            return;
        }

        // Map experiment category to recommendation type
        var recommendationType = MapCategoryToRecommendationType(experiment.Category);
        if (recommendationType == null)
        {
            _logger.LogDebug(
                "Experiment category {Category} does not map to a trackable recommendation type",
                experiment.Category);
            return;
        }

        // Get or create playbook
        var playbook = await _playbookRepository.GetByUserIdAsync(notification.UserId, cancellationToken);
        if (playbook == null)
        {
            playbook = UserPlaybook.Create(notification.UserId);
            await _playbookRepository.AddAsync(playbook, cancellationToken);
        }

        // Build context key for the experiment
        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var contextKey = BuildExperimentContextKey(today);

        // Calculate success signal based on experiment outcome
        var successSignal = MapOutcomeToSuccessSignal(notification.Outcome);

        var entry = playbook.GetOrCreateEntry(recommendationType.Value, contextKey.ToStorageKey());

        // Apply experiment outcome with higher weight (experiments are explicit tests)
        var amplifiedSignal = Math.Min(1.0m, successSignal * ExperimentLearningRateMultiplier);
        entry.UpdateWeight(amplifiedSignal);

        // Also update completion rate if experiment was positive
        if (notification.Outcome == ExperimentOutcome.Positive)
        {
            entry.UpdateRates(wasAccepted: true, wasCompleted: true);
        }
        else if (notification.Outcome == ExperimentOutcome.Negative)
        {
            entry.UpdateRates(wasAccepted: true, wasCompleted: false);
        }

        await _playbookRepository.UpdateAsync(playbook, cancellationToken);

        _logger.LogInformation(
            "Updated playbook for user {UserId} based on experiment {ExperimentId}: " +
            "category={Category}, outcome={Outcome}, type={Type}, signal={Signal:F2}",
            notification.UserId,
            notification.ExperimentId,
            experiment.Category,
            notification.Outcome,
            recommendationType,
            successSignal);
    }

    /// <summary>
    /// Maps experiment category to the recommendation type that would suggest such an experiment.
    /// </summary>
    private static RecommendationType? MapCategoryToRecommendationType(ExperimentCategory category)
    {
        return category switch
        {
            ExperimentCategory.Habit => RecommendationType.HabitModeSuggestion,
            ExperimentCategory.Routine => RecommendationType.ScheduleAdjustmentSuggestion,
            ExperimentCategory.PlanRealism => RecommendationType.PlanRealismAdjustment,
            ExperimentCategory.FrictionReduction => RecommendationType.TaskBreakdownSuggestion,
            ExperimentCategory.Productivity => RecommendationType.NextBestAction,
            ExperimentCategory.Top1FollowThrough => RecommendationType.Top1Suggestion,
            ExperimentCategory.CheckInConsistency => RecommendationType.CheckInConsistencyNudge,
            _ => RecommendationType.ExperimentRecommendation // Default for experiments
        };
    }

    /// <summary>
    /// Maps experiment outcome to a success signal (0-1).
    /// </summary>
    private static decimal MapOutcomeToSuccessSignal(ExperimentOutcome outcome)
    {
        return outcome switch
        {
            ExperimentOutcome.Positive => 1.0m,       // Strong positive signal
            ExperimentOutcome.Neutral => 0.5m,        // No change, neutral signal
            ExperimentOutcome.Negative => 0.1m,       // Negative signal (avoid this)
            ExperimentOutcome.Inconclusive => 0.4m,   // Slightly below neutral (might not work)
            _ => 0.5m
        };
    }

    /// <summary>
    /// Builds a context key for the experiment.
    /// Since experiments run over time, we use a simplified context.
    /// </summary>
    private static ContextKey BuildExperimentContextKey(DateOnly date)
    {
        // Use medium defaults since experiments span multiple days/contexts
        return ContextKey.FromValues(
            energyLevel: 3,
            capacityUtilization: 1.0m,
            dayOfWeek: date.DayOfWeek,
            seasonIntensity: 3);
    }
}

/// <summary>
/// Updates the user's playbook when an experiment is abandoned.
/// Abandoned experiments may indicate friction or that the approach doesn't work.
/// </summary>
public sealed class ExperimentAbandonedEventHandler(
    IExperimentRepository _experimentRepository,
    IUserPlaybookRepository _playbookRepository,
    IDateTimeProvider _dateTimeProvider,
    ILogger<ExperimentAbandonedEventHandler> _logger)
    : INotificationHandler<ExperimentAbandonedEvent>
{
    public async Task Handle(ExperimentAbandonedEvent notification, CancellationToken cancellationToken)
    {
        var experiment = await _experimentRepository.GetByIdAsync(notification.ExperimentId, cancellationToken);
        if (experiment == null)
            return;

        // Map experiment category to recommendation type
        var recommendationType = MapCategoryToRecommendationType(experiment.Category);
        if (recommendationType == null)
            return;

        // Get or create playbook
        var playbook = await _playbookRepository.GetByUserIdAsync(notification.UserId, cancellationToken);
        if (playbook == null)
        {
            playbook = UserPlaybook.Create(notification.UserId);
            await _playbookRepository.AddAsync(playbook, cancellationToken);
        }

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var contextKey = ContextKey.FromValues(
            energyLevel: 3,
            capacityUtilization: 1.0m,
            dayOfWeek: today.DayOfWeek,
            seasonIntensity: 3);

        var entry = playbook.GetOrCreateEntry(recommendationType.Value, contextKey.ToStorageKey());

        // Abandonment is a weak negative signal (0.3 - indicates friction)
        entry.UpdateWeight(0.3m);
        entry.UpdateRates(wasAccepted: true, wasCompleted: false);

        await _playbookRepository.UpdateAsync(playbook, cancellationToken);

        _logger.LogInformation(
            "Updated playbook for user {UserId} based on abandoned experiment {ExperimentId}",
            notification.UserId,
            notification.ExperimentId);
    }

    private static RecommendationType? MapCategoryToRecommendationType(ExperimentCategory category)
    {
        return category switch
        {
            ExperimentCategory.Habit => RecommendationType.HabitModeSuggestion,
            ExperimentCategory.Routine => RecommendationType.ScheduleAdjustmentSuggestion,
            ExperimentCategory.PlanRealism => RecommendationType.PlanRealismAdjustment,
            ExperimentCategory.FrictionReduction => RecommendationType.TaskBreakdownSuggestion,
            ExperimentCategory.Productivity => RecommendationType.NextBestAction,
            ExperimentCategory.Top1FollowThrough => RecommendationType.Top1Suggestion,
            ExperimentCategory.CheckInConsistency => RecommendationType.CheckInConsistencyNudge,
            _ => RecommendationType.ExperimentRecommendation
        };
    }
}

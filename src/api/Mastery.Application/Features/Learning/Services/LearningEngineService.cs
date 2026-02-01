using System.Text.Json;
using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Learning;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Mastery.Application.Features.Learning.Services;

/// <summary>
/// Learning engine that updates user playbooks based on recommendation outcomes.
/// Implements a simple multi-armed bandit for personalized intervention weighting.
/// </summary>
public interface ILearningEngineService
{
    /// <summary>
    /// Records an outcome with explicit context information.
    /// </summary>
    Task RecordOutcomeWithContextAsync(
        Recommendation recommendation,
        bool wasAccepted,
        bool? wasCompleted,
        string? dismissReason,
        ContextKey context,
        CancellationToken ct = default);

    /// <summary>
    /// Records actual completion status after a task/habit is completed.
    /// Updates the intervention outcome and playbook for the accepted recommendation.
    /// </summary>
    Task RecordActualCompletionAsync(
        Guid recommendationId,
        bool wasCompleted,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the learned weight for a recommendation type in a context.
    /// </summary>
    Task<decimal> GetWeightAsync(
        string userId,
        RecommendationType recommendationType,
        ContextKey context,
        CancellationToken ct = default);

    /// <summary>
    /// Gets learned weights for multiple recommendation types (batch query).
    /// Returns a dictionary mapping RecommendationType to weight (0.1 to 0.95).
    /// </summary>
    Task<IReadOnlyDictionary<RecommendationType, decimal>> GetWeightsForTypesAsync(
        string userId,
        IEnumerable<RecommendationType> types,
        ContextKey context,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the user's full playbook.
    /// </summary>
    Task<UserPlaybook?> GetPlaybookAsync(string userId, CancellationToken ct = default);
}

public sealed class LearningEngineService(
    IUserPlaybookRepository _playbookRepository,
    IInterventionOutcomeRepository _outcomeRepository,
    ILogger<LearningEngineService> _logger)
    : ILearningEngineService
{
    /// <inheritdoc />
    public async Task RecordActualCompletionAsync(
        Guid recommendationId,
        bool wasCompleted,
        CancellationToken ct = default)
    {
        var outcome = await _outcomeRepository.GetByRecommendationIdSingleAsync(recommendationId, ct);
        if (outcome == null)
        {
            _logger.LogWarning(
                "No intervention outcome found for recommendation {RecommendationId} to update completion",
                recommendationId);
            return;
        }

        // Skip if already has a completion status set
        if (outcome.WasCompleted.HasValue)
        {
            _logger.LogWarning(
                "Intervention outcome for recommendation {RecommendationId} already has completion status set",
                recommendationId);
            return;
        }

        // Update the outcome
        outcome.RecordCompletion(wasCompleted);
        await _outcomeRepository.UpdateAsync(outcome, ct);

        // Update the playbook with the new completion signal
        var playbook = await _playbookRepository.GetByUserIdAsync(outcome.UserId, ct);
        if (playbook == null)
        {
            _logger.LogWarning(
                "No playbook found for user {UserId} to update completion",
                outcome.UserId);
            return;
        }

        var entry = playbook.GetOrCreateEntry(outcome.RecommendationType, outcome.ContextKey);

        // Calculate the new success signal based on completion
        // Completion = 1.0, non-completion = 0.3 (was accepted, so partial credit)
        var successSignal = wasCompleted ? 1.0m : 0.3m;
        entry.UpdateCompletionOnly(wasCompleted, successSignal);

        await _playbookRepository.UpdateAsync(playbook, ct);

        _logger.LogInformation(
            "Updated completion for recommendation {RecommendationId}: completed={Completed}",
            recommendationId, wasCompleted);
    }

    /// <inheritdoc />
    public async Task RecordOutcomeWithContextAsync(
        Recommendation recommendation,
        bool wasAccepted,
        bool? wasCompleted,
        string? dismissReason,
        ContextKey context,
        CancellationToken ct = default)
    {
        var userId = recommendation.UserId;
        var contextKeyStr = context.ToStorageKey();

        _logger.LogDebug(
            "Recording outcome for recommendation {RecommendationId}: accepted={Accepted}, completed={Completed}",
            recommendation.Id, wasAccepted, wasCompleted);

        // Extract intervention code from ActionPayload if available
        var interventionCode = ExtractInterventionCode(recommendation);

        // Create outcome record
        var outcome = InterventionOutcome.Create(
            userId: userId,
            recommendationId: recommendation.Id,
            recommendationType: recommendation.Type,
            interventionCode: interventionCode,
            contextKey: contextKeyStr,
            originalScore: recommendation.Score,
            energyLevel: (int)context.Energy + 1, // Convert back to 1-5 scale approximately
            capacityUtilization: context.Capacity switch
            {
                CapacityBucket.Overloaded => 1.3m,
                CapacityBucket.Full => 1.0m,
                _ => 0.6m
            },
            dayOfWeek: context.DayType == DayTypeBucket.Weekend ? DayOfWeek.Saturday : DayOfWeek.Monday,
            seasonIntensity: (int)context.SeasonIntensity + 1);

        if (wasAccepted)
        {
            outcome.RecordAcceptance();
            if (wasCompleted.HasValue)
                outcome.RecordCompletion(wasCompleted.Value);
        }
        else
        {
            outcome.RecordDismissal(dismissReason);
        }

        await _outcomeRepository.AddAsync(outcome, ct);

        // Update playbook
        var playbook = await _playbookRepository.GetByUserIdAsync(userId, ct);
        if (playbook == null)
        {
            playbook = UserPlaybook.Create(userId);
            await _playbookRepository.AddAsync(playbook, ct);
        }

        playbook.RecordOutcome(outcome);

        // Update rates on the entry
        var entry = playbook.GetOrCreateEntry(recommendation.Type, contextKeyStr);
        entry.UpdateRates(wasAccepted, wasCompleted);

        await _playbookRepository.UpdateAsync(playbook, ct);

        _logger.LogInformation(
            "Updated playbook for user {UserId}: type={Type}, context={Context}, newWeight={Weight:F3}",
            userId, recommendation.Type, contextKeyStr, entry.SuccessWeight);
    }

    public async Task<decimal> GetWeightAsync(
        string userId,
        RecommendationType recommendationType,
        ContextKey context,
        CancellationToken ct = default)
    {
        var playbook = await _playbookRepository.GetByUserIdAsync(userId, ct);
        if (playbook == null)
            return 0.5m; // Default neutral weight

        var contextKeyStr = context.ToStorageKey();
        var weight = playbook.GetWeight(recommendationType, contextKeyStr);

        // If no exact match, try to find a similar context
        if (weight == 0.5m && playbook.Entries.Count > 0)
        {
            var entriesForType = playbook.GetEntriesForType(recommendationType).ToList();
            if (entriesForType.Count > 0)
            {
                // Average weight across all contexts for this type
                weight = entriesForType.Average(e => e.SuccessWeight);
            }
        }

        return weight;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<RecommendationType, decimal>> GetWeightsForTypesAsync(
        string userId,
        IEnumerable<RecommendationType> types,
        ContextKey context,
        CancellationToken ct = default)
    {
        var playbook = await _playbookRepository.GetByUserIdAsync(userId, ct);
        var contextKeyStr = context.ToStorageKey();
        var result = new Dictionary<RecommendationType, decimal>();

        foreach (var type in types)
        {
            if (playbook == null)
            {
                result[type] = 0.5m; // Default neutral weight
                continue;
            }

            var weight = playbook.GetWeight(type, contextKeyStr);

            // If no exact match, try to find average for this type
            if (weight == 0.5m && playbook.Entries.Count > 0)
            {
                var entriesForType = playbook.GetEntriesForType(type).ToList();
                if (entriesForType.Count > 0)
                {
                    weight = entriesForType.Average(e => e.SuccessWeight);
                }
            }

            result[type] = weight;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<UserPlaybook?> GetPlaybookAsync(string userId, CancellationToken ct = default)
    {
        return await _playbookRepository.GetByUserIdAsync(userId, ct);
    }

    /// <summary>
    /// Extracts the intervention code from a recommendation's ActionPayload or derives it from the type.
    /// </summary>
    private string? ExtractInterventionCode(Recommendation recommendation)
    {
        // Try to extract from ActionPayload JSON
        if (!string.IsNullOrEmpty(recommendation.ActionPayload))
        {
            try
            {
                using var doc = JsonDocument.Parse(recommendation.ActionPayload);
                var root = doc.RootElement;

                // Check for explicit intervention code in payload
                if (root.TryGetProperty("interventionCode", out var codeElement) ||
                    root.TryGetProperty("InterventionCode", out codeElement) ||
                    root.TryGetProperty("code", out codeElement))
                {
                    var code = codeElement.GetString();
                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        _logger.LogDebug(
                            "Extracted intervention code '{Code}' from recommendation {RecommendationId}",
                            code, recommendation.Id);
                        return code;
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogDebug(
                    ex,
                    "Failed to parse ActionPayload for recommendation {RecommendationId}",
                    recommendation.Id);
            }
        }

        // Fall back to deriving code from recommendation type and action kind
        var derivedCode = DeriveInterventionCode(recommendation.Type, recommendation.ActionKind);

        if (!string.IsNullOrEmpty(derivedCode))
        {
            _logger.LogDebug(
                "Derived intervention code '{Code}' from type {Type} and action {ActionKind}",
                derivedCode, recommendation.Type, recommendation.ActionKind);
        }

        return derivedCode;
    }

    /// <summary>
    /// Derives an intervention code from the recommendation type and action kind.
    /// </summary>
    private static string? DeriveInterventionCode(RecommendationType type, RecommendationActionKind actionKind)
    {
        // Map recommendation types to canonical intervention codes
        return (type, actionKind) switch
        {
            (RecommendationType.NextBestAction, _) => "NBA_SUGGEST",
            (RecommendationType.Top1Suggestion, _) => "TOP1_SELECT",
            (RecommendationType.HabitModeSuggestion, _) => "HABIT_MODE_SCALE",
            (RecommendationType.PlanRealismAdjustment, _) => "PLAN_ADJUST",
            (RecommendationType.TaskBreakdownSuggestion, _) => "TASK_BREAKDOWN",
            (RecommendationType.ScheduleAdjustmentSuggestion, _) => "SCHEDULE_ADJUST",
            (RecommendationType.ProjectStuckFix, _) => "PROJECT_UNSTICK",
            (RecommendationType.ExperimentRecommendation, RecommendationActionKind.Create) => "EXPERIMENT_START",
            (RecommendationType.ExperimentRecommendation, RecommendationActionKind.Update) => "EXPERIMENT_MODIFY",
            (RecommendationType.GoalScoreboardSuggestion, _) => "GOAL_SCOREBOARD",
            (RecommendationType.HabitFromLeadMetricSuggestion, _) => "HABIT_FROM_METRIC",
            (RecommendationType.CheckInConsistencyNudge, _) => "CHECKIN_NUDGE",
            (RecommendationType.MetricObservationReminder, _) => "METRIC_REMIND",
            (RecommendationType.TaskEditSuggestion, _) => "TASK_EDIT",
            (RecommendationType.TaskArchiveSuggestion, _) => "TASK_ARCHIVE",
            (RecommendationType.TaskTriageSuggestion, _) => "TASK_TRIAGE",
            (RecommendationType.HabitEditSuggestion, _) => "HABIT_EDIT",
            (RecommendationType.HabitArchiveSuggestion, _) => "HABIT_ARCHIVE",
            (RecommendationType.GoalEditSuggestion, _) => "GOAL_EDIT",
            (RecommendationType.GoalArchiveSuggestion, _) => "GOAL_ARCHIVE",
            (RecommendationType.ProjectSuggestion, _) => "PROJECT_CREATE",
            (RecommendationType.ProjectEditSuggestion, _) => "PROJECT_EDIT",
            (RecommendationType.ProjectArchiveSuggestion, _) => "PROJECT_ARCHIVE",
            (RecommendationType.ProjectGoalLinkSuggestion, _) => "PROJECT_GOAL_LINK",
            (RecommendationType.MetricEditSuggestion, _) => "METRIC_EDIT",
            (RecommendationType.ExperimentEditSuggestion, _) => "EXPERIMENT_EDIT",
            (RecommendationType.ExperimentArchiveSuggestion, _) => "EXPERIMENT_ARCHIVE",
            _ => null
        };
    }
}

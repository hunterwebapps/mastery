using System.Text.Json;
using Mastery.Application.Common.Models;
using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Mastery.Infrastructure.Services;

/// <summary>
/// Validates recommendation candidates against user state to filter out
/// candidates with hallucinated entity IDs.
/// </summary>
internal static class RecommendationCandidateValidator
{
    /// <summary>
    /// Filters out recommendation candidates that reference non-existent entity IDs.
    /// </summary>
    public static List<RecommendationCandidate> FilterInvalidEntityIds(
        List<RecommendationCandidate> candidates,
        UserStateSnapshot state,
        ILogger logger)
    {
        var validTaskIds = state.Tasks.Select(t => t.Id).ToHashSet();
        var validHabitIds = state.Habits.Select(h => h.Id).ToHashSet();
        var validGoalIds = state.Goals.Select(g => g.Id).ToHashSet();
        var validProjectIds = state.Projects.Select(p => p.Id).ToHashSet();
        var validMetricIds = state.MetricDefinitions.Select(m => m.Id).ToHashSet();
        var validExperimentIds = state.Experiments.Select(e => e.Id).ToHashSet();

        var validSets = new ValidIdSets(
            validTaskIds, validHabitIds, validGoalIds,
            validProjectIds, validMetricIds, validExperimentIds);

        var validated = new List<RecommendationCandidate>();

        foreach (var candidate in candidates)
        {
            if (ValidateCandidate(candidate, validSets, logger))
                validated.Add(candidate);
        }

        if (validated.Count < candidates.Count)
        {
            logger.LogWarning(
                "Filtered {FilteredCount} recommendation(s) with invalid entity IDs (kept {KeptCount})",
                candidates.Count - validated.Count, validated.Count);
        }

        return validated;
    }

    private static bool ValidateCandidate(
        RecommendationCandidate candidate,
        ValidIdSets validIds,
        ILogger logger)
    {
        // Create actions with null targetEntityId are always valid
        if (candidate.ActionKind == RecommendationActionKind.Create && candidate.Target.EntityId is null)
            return true;

        // Reflection prompts don't reference entities
        if (candidate.ActionKind is RecommendationActionKind.ReflectPrompt or RecommendationActionKind.LearnPrompt)
            return true;

        // For Update/Remove/ExecuteToday/Defer: validate target entity ID
        if (candidate.Target.EntityId.HasValue)
        {
            var isValid = candidate.Target.Kind switch
            {
                RecommendationTargetKind.Task => validIds.TaskIds.Contains(candidate.Target.EntityId.Value),
                RecommendationTargetKind.Habit => validIds.HabitIds.Contains(candidate.Target.EntityId.Value),
                RecommendationTargetKind.Goal => validIds.GoalIds.Contains(candidate.Target.EntityId.Value),
                RecommendationTargetKind.Project => validIds.ProjectIds.Contains(candidate.Target.EntityId.Value),
                RecommendationTargetKind.Metric => validIds.MetricIds.Contains(candidate.Target.EntityId.Value),
                RecommendationTargetKind.Experiment => validIds.ExperimentIds.Contains(candidate.Target.EntityId.Value),
                RecommendationTargetKind.UserProfile => true, // UserProfile doesn't need validation
                _ => true
            };

            if (!isValid)
            {
                logger.LogWarning(
                    "Filtered recommendation '{Title}' with invalid {Kind} target ID {EntityId}",
                    candidate.Title, candidate.Target.Kind, candidate.Target.EntityId);
                return false;
            }
        }

        // Also validate IDs embedded in ActionPayload
        if (!string.IsNullOrEmpty(candidate.ActionPayload))
        {
            if (!ValidateActionPayloadIds(candidate, validIds, logger))
                return false;
        }

        return true;
    }

    private static bool ValidateActionPayloadIds(
        RecommendationCandidate candidate,
        ValidIdSets validIds,
        ILogger logger)
    {
        JsonDocument? doc = null;
        try
        {
            doc = JsonDocument.Parse(candidate.ActionPayload!);
            var root = doc.RootElement;

            // Validate taskId
            if (TryGetGuid(root, "taskId", out var taskId) && !validIds.TaskIds.Contains(taskId))
            {
                logger.LogWarning(
                    "Filtered recommendation '{Title}' with invalid taskId {TaskId} in payload",
                    candidate.Title, taskId);
                return false;
            }

            // Validate habitId
            if (TryGetGuid(root, "habitId", out var habitId) && !validIds.HabitIds.Contains(habitId))
            {
                logger.LogWarning(
                    "Filtered recommendation '{Title}' with invalid habitId {HabitId} in payload",
                    candidate.Title, habitId);
                return false;
            }

            // Validate goalId (skip null - that's allowed for Create actions)
            if (TryGetGuid(root, "goalId", out var goalId) && !validIds.GoalIds.Contains(goalId))
            {
                // goalId can be optional in Create payloads, but if specified must be valid
                logger.LogWarning(
                    "Filtered recommendation '{Title}' with invalid goalId {GoalId} in payload",
                    candidate.Title, goalId);
                return false;
            }

            // Validate projectId (skip null - that's allowed for Create actions)
            if (TryGetGuid(root, "projectId", out var projectId) && !validIds.ProjectIds.Contains(projectId))
            {
                logger.LogWarning(
                    "Filtered recommendation '{Title}' with invalid projectId {ProjectId} in payload",
                    candidate.Title, projectId);
                return false;
            }

            // Validate metricId / metricDefinitionId
            if (TryGetGuid(root, "metricId", out var metricId) && !validIds.MetricIds.Contains(metricId))
            {
                logger.LogWarning(
                    "Filtered recommendation '{Title}' with invalid metricId {MetricId} in payload",
                    candidate.Title, metricId);
                return false;
            }
            if (TryGetGuid(root, "metricDefinitionId", out var metricDefId) && !validIds.MetricIds.Contains(metricDefId))
            {
                logger.LogWarning(
                    "Filtered recommendation '{Title}' with invalid metricDefinitionId {MetricId} in payload",
                    candidate.Title, metricDefId);
                return false;
            }

            // Validate experimentId
            if (TryGetGuid(root, "experimentId", out var experimentId) && !validIds.ExperimentIds.Contains(experimentId))
            {
                logger.LogWarning(
                    "Filtered recommendation '{Title}' with invalid experimentId {ExperimentId} in payload",
                    candidate.Title, experimentId);
                return false;
            }

            // Validate suggestedNextTaskId (for ProjectStuckFix)
            if (TryGetGuid(root, "suggestedNextTaskId", out var nextTaskId) && !validIds.TaskIds.Contains(nextTaskId))
            {
                logger.LogWarning(
                    "Filtered recommendation '{Title}' with invalid suggestedNextTaskId {TaskId} in payload",
                    candidate.Title, nextTaskId);
                return false;
            }

            // Validate newGoalId (for ProjectEditSuggestion)
            if (TryGetGuid(root, "newGoalId", out var newGoalId) && !validIds.GoalIds.Contains(newGoalId))
            {
                logger.LogWarning(
                    "Filtered recommendation '{Title}' with invalid newGoalId {GoalId} in payload",
                    candidate.Title, newGoalId);
                return false;
            }

            return true;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse ActionPayload for '{Title}'", candidate.Title);
            return false;
        }
        finally
        {
            doc?.Dispose();
        }
    }

    private static bool TryGetGuid(JsonElement root, string propertyName, out Guid value)
    {
        value = Guid.Empty;
        if (!root.TryGetProperty(propertyName, out var prop))
            return false;
        if (prop.ValueKind == JsonValueKind.Null)
            return false;
        if (prop.ValueKind != JsonValueKind.String)
            return false;
        var str = prop.GetString();
        if (string.IsNullOrWhiteSpace(str))
            return false;
        return Guid.TryParse(str, out value);
    }

    private sealed record ValidIdSets(
        HashSet<Guid> TaskIds,
        HashSet<Guid> HabitIds,
        HashSet<Guid> GoalIds,
        HashSet<Guid> ProjectIds,
        HashSet<Guid> MetricIds,
        HashSet<Guid> ExperimentIds);
}

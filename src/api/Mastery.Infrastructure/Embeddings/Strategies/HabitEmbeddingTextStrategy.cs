using System.Text;
using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Goal;
using Mastery.Domain.Entities.Habit;
using Mastery.Domain.Entities.Metrics;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;
using Mastery.Domain.ValueObjects;

namespace Mastery.Infrastructure.Embeddings.Strategies;

/// <summary>
/// Embedding text strategy for Habit entities.
/// Context depth: Self + Goals (Title/Description) + User Roles/Values + Metric bindings + Policy.
/// </summary>
public sealed class HabitEmbeddingTextStrategy(
    IGoalRepository _goalRepository,
    IUserProfileRepository _userProfileRepository,
    IMetricDefinitionRepository _metricDefinitionRepository) : IEmbeddingTextStrategy<Habit>
{
    public async Task<string?> CompileTextAsync(Habit entity, CancellationToken ct)
    {
        var sb = new StringBuilder();

        // Build leading summary: "{Title} - {Schedule} habit supporting {linked goals count} goals"
        var goalsCount = entity.GoalIds.Count;
        var goalsSuffix = goalsCount > 0 ? $" supporting {goalsCount} goal{(goalsCount > 1 ? "s" : "")}" : "";
        EmbeddingFormatHelper.AppendSummary(sb, "HABIT",
            $"{entity.Title} - {FormatSchedule(entity.Schedule)} habit{goalsSuffix}");

        // Basic habit information
        sb.AppendLine($"Title: {entity.Title}");
        EmbeddingFormatHelper.AppendFieldIfNotEmpty(sb, "Description", entity.Description);
        EmbeddingFormatHelper.AppendFieldIfNotEmpty(sb, "Why", entity.Why);

        sb.AppendLine($"Status: {EmbeddingFormatHelper.FormatEnum(entity.Status)}");
        sb.AppendLine($"Default mode: {FormatHabitMode(entity.DefaultMode)}");
        sb.AppendLine($"Schedule: {FormatSchedule(entity.Schedule)}");
        sb.AppendLine($"Policy: {FormatPolicy(entity.Policy)}");

        // Include user values and roles associated with this habit
        await AppendUserContextAsync(sb, entity, ct);

        // Include linked goals
        await AppendGoalsAsync(sb, entity, ct);

        // Include metric bindings (simplified)
        await AppendMetricBindingsAsync(sb, entity, ct);

        // Include variants prominently - this is a key product differentiator (minimum version scaling)
        AppendVariants(sb, entity);

        // Include performance stats if meaningful
        AppendPerformanceStats(sb, entity);

        // Domain keywords for semantic search
        EmbeddingFormatHelper.AppendKeywords(sb,
            "habit", "behavior", "adherence", "routine", "trigger", "reward",
            "minimum version", "scaling", "streak", "consistency", "daily practice");

        return sb.ToString();
    }

    private async Task AppendUserContextAsync(StringBuilder sb, Habit entity, CancellationToken ct)
    {
        var userProfile = await _userProfileRepository.GetByUserIdAsync(entity.UserId, ct);
        if (userProfile == null) return;

        // Append associated values (simplified)
        if (entity.ValueIds.Count > 0)
        {
            var associatedValues = userProfile.Values
                .Where(v => entity.ValueIds.Contains(v.Id))
                .OrderBy(v => v.Rank)
                .Select(v => v.Label)
                .ToList();

            if (associatedValues.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"Values: {string.Join(", ", associatedValues)}");
            }
        }

        // Append associated roles (simplified)
        if (entity.RoleIds.Count > 0)
        {
            var associatedRoles = userProfile.Roles
                .Where(r => entity.RoleIds.Contains(r.Id))
                .OrderBy(r => r.Rank)
                .Select(r => r.Label)
                .ToList();

            if (associatedRoles.Count > 0)
            {
                sb.AppendLine($"Roles: {string.Join(", ", associatedRoles)}");
            }
        }
    }

    private async Task AppendGoalsAsync(StringBuilder sb, Habit entity, CancellationToken ct)
    {
        if (entity.GoalIds.Count == 0) return;

        var goals = new List<Goal>();
        foreach (var goalId in entity.GoalIds)
        {
            var goal = await _goalRepository.GetByIdAsync(goalId, ct);
            if (goal != null)
            {
                goals.Add(goal);
            }
        }

        if (goals.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Contributing to Goals:");
            foreach (var goal in goals)
            {
                sb.AppendLine($"  - {goal.Title} ({EmbeddingFormatHelper.FormatEnum(goal.Status)}, Priority {goal.Priority}/5)");
                if (!string.IsNullOrWhiteSpace(goal.Why))
                {
                    sb.AppendLine($"    Why: {goal.Why}");
                }
            }
        }
    }

    private async Task AppendMetricBindingsAsync(StringBuilder sb, Habit entity, CancellationToken ct)
    {
        if (!entity.HasMetricBindings) return;

        var metricIds = entity.MetricBindings.Select(b => b.MetricDefinitionId).ToList();
        var definitions = await _metricDefinitionRepository.GetByIdsAsync(metricIds, ct);
        var definitionMap = definitions.ToDictionary(d => d.Id);

        sb.AppendLine();
        sb.AppendLine("Metrics affected by completion:");
        foreach (var binding in entity.MetricBindings)
        {
            if (definitionMap.TryGetValue(binding.MetricDefinitionId, out var definition))
            {
                var contribution = FormatContributionType(binding.ContributionType);
                var fixedValue = binding.FixedValue.HasValue ? $" ({binding.FixedValue})" : "";
                sb.AppendLine($"  - {definition.Name}: {contribution}{fixedValue}");
            }
        }
    }

    /// <summary>
    /// Prominently displays habit variants (minimum version scaling) - a key product differentiator.
    /// </summary>
    private static void AppendVariants(StringBuilder sb, Habit entity)
    {
        if (!entity.HasVariants) return;

        sb.AppendLine();
        sb.AppendLine("SCALING OPTIONS (Minimum Version Support):");
        foreach (var variant in entity.Variants.OrderBy(v => v.Mode))
        {
            var modeLabel = FormatHabitMode(variant.Mode);
            var countsAs = variant.CountsAsCompletion ? "counts as done" : "partial credit";
            sb.AppendLine($"  {modeLabel}: {variant.Label}");
            sb.AppendLine($"    {variant.EstimatedMinutes} min, {FormatEnergyCost(variant.EnergyCost)} energy, {countsAs}");
        }
    }

    private static void AppendPerformanceStats(StringBuilder sb, Habit entity)
    {
        if (entity.CurrentStreak <= 0 && entity.AdherenceRate7Day <= 0) return;

        sb.AppendLine();
        sb.AppendLine("Performance:");
        if (entity.CurrentStreak > 0)
        {
            sb.AppendLine($"  Current streak: {entity.CurrentStreak} days");
        }
        if (entity.AdherenceRate7Day > 0)
        {
            sb.AppendLine($"  7-day adherence: {entity.AdherenceRate7Day:F0}%");
        }
    }

    private static string FormatSchedule(HabitSchedule schedule) => schedule.Type switch
    {
        ScheduleType.Daily => "Daily",
        ScheduleType.DaysOfWeek when schedule.DaysOfWeek != null =>
            $"On {string.Join(", ", schedule.DaysOfWeek.Select(d => d.ToString()))}",
        ScheduleType.WeeklyFrequency when schedule.FrequencyPerWeek.HasValue =>
            $"{schedule.FrequencyPerWeek} times per week",
        ScheduleType.Interval when schedule.IntervalDays.HasValue =>
            $"Every {schedule.IntervalDays} days",
        _ => schedule.ToString()
    };

    private static string FormatHabitMode(HabitMode mode) => mode switch
    {
        HabitMode.Full => "Full",
        HabitMode.Maintenance => "Maintenance (reduced version)",
        HabitMode.Minimum => "Minimum (bare minimum to keep streak)",
        _ => EmbeddingFormatHelper.FormatEnum(mode)
    };

    private static string FormatPolicy(HabitPolicy policy)
    {
        // HabitPolicy is a ValueObject with various settings
        var parts = new List<string>();

        if (!policy.AllowLateCompletion && !policy.AllowSkip && policy.RequireMissReason)
            return "Strict (must complete, no late, no skipping)";

        if (policy.AllowLateCompletion)
            parts.Add("late OK");
        if (policy.AllowSkip)
            parts.Add("can skip");
        if (policy.AllowBackfill)
            parts.Add($"backfill up to {policy.MaxBackfillDays} days");

        return parts.Count > 0 ? $"Flexible ({string.Join(", ", parts)})" : "Default";
    }

    private static string FormatContributionType(HabitContributionType type) => type switch
    {
        HabitContributionType.BooleanAs1 => "adds 1 on completion",
        HabitContributionType.FixedValue => "adds fixed value",
        HabitContributionType.UseEnteredValue => "uses entered value",
        _ => EmbeddingFormatHelper.FormatEnum(type)
    };

    private static string FormatEnergyCost(int cost) => cost switch
    {
        1 => "very low",
        2 => "low",
        3 => "medium",
        4 => "high",
        5 => "very high",
        _ => $"{cost}/5"
    };
}

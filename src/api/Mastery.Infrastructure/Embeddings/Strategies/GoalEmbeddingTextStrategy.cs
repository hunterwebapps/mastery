using System.Text;
using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Goal;
using Mastery.Domain.Entities.Metrics;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;

namespace Mastery.Infrastructure.Embeddings.Strategies;

/// <summary>
/// Embedding text strategy for Goal entities.
/// Context depth: Self + Metrics definitions (simplified) + User Roles/Values + Dependencies.
/// </summary>
public sealed class GoalEmbeddingTextStrategy(
    IMetricDefinitionRepository _metricDefinitionRepository,
    IUserProfileRepository _userProfileRepository,
    IGoalRepository _goalRepository) : IEmbeddingTextStrategy<Goal>
{
    public async Task<string?> CompileTextAsync(Goal entity, CancellationToken ct)
    {
        var sb = new StringBuilder();

        // Build leading summary: "{Title} - {Why} (Priority {X}/5, deadline {date})"
        var deadlinePart = entity.Deadline.HasValue
            ? $", deadline {EmbeddingFormatHelper.FormatDate(entity.Deadline.Value)}"
            : "";
        var whyPart = !string.IsNullOrWhiteSpace(entity.Why) ? entity.Why : entity.Description ?? entity.Title;
        EmbeddingFormatHelper.AppendSummary(sb, "GOAL",
            $"{entity.Title} - {whyPart} (Priority {EmbeddingFormatHelper.FormatPriority(entity.Priority)}{deadlinePart})");

        // Basic goal information
        sb.AppendLine($"Title: {entity.Title}");
        EmbeddingFormatHelper.AppendFieldIfNotEmpty(sb, "Description", entity.Description);
        EmbeddingFormatHelper.AppendFieldIfNotEmpty(sb, "Why", entity.Why);

        sb.AppendLine($"Status: {EmbeddingFormatHelper.FormatEnum(entity.Status)}");
        sb.AppendLine($"Priority: {EmbeddingFormatHelper.FormatPriority(entity.Priority)}");
        EmbeddingFormatHelper.AppendDateFieldIfPresent(sb, "Deadline", entity.Deadline);

        // Include user values and roles associated with this goal
        await AppendUserContextAsync(sb, entity, ct);

        // Include dependencies
        await AppendDependenciesAsync(sb, entity, ct);

        // Include metrics from the scoreboard (simplified output)
        await AppendScoreboardAsync(sb, entity, ct);

        // Domain keywords for semantic search
        EmbeddingFormatHelper.AppendKeywords(sb,
            "goal", "outcome", "lead metric", "lag metric", "constraint", "target",
            "baseline", "setpoint", "objective", "achievement", "progress", "drift");

        return sb.ToString();
    }

    private async Task AppendUserContextAsync(StringBuilder sb, Goal entity, CancellationToken ct)
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

    private async Task AppendDependenciesAsync(StringBuilder sb, Goal entity, CancellationToken ct)
    {
        if (entity.DependencyIds.Count == 0) return;

        var dependencyTitles = new List<string>();
        foreach (var depId in entity.DependencyIds)
        {
            var depGoal = await _goalRepository.GetByIdAsync(depId, ct);
            if (depGoal != null)
            {
                dependencyTitles.Add($"{depGoal.Title} ({EmbeddingFormatHelper.FormatEnum(depGoal.Status)})");
            }
        }

        if (dependencyTitles.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"Dependencies: {string.Join("; ", dependencyTitles)}");
        }
    }

    private async Task AppendScoreboardAsync(StringBuilder sb, Goal entity, CancellationToken ct)
    {
        if (!entity.HasScoreboard) return;

        var metricIds = entity.Metrics.Select(m => m.MetricDefinitionId).ToList();
        var definitions = await _metricDefinitionRepository.GetByIdsAsync(metricIds, ct);
        var definitionMap = definitions.ToDictionary(d => d.Id);

        // Lag metric (outcome) - simplified: name, description, direction only
        var lagMetric = entity.GetLagMetric();
        if (lagMetric != null && definitionMap.TryGetValue(lagMetric.MetricDefinitionId, out var lagDef))
        {
            sb.AppendLine();
            sb.AppendLine("Lag Metric (Outcome):");
            AppendSimplifiedMetric(sb, lagMetric, lagDef);
        }

        // Lead metrics (leading indicators)
        var leadMetrics = entity.GetLeadMetrics().ToList();
        if (leadMetrics.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Lead Metrics (Leading Indicators):");
            foreach (var lead in leadMetrics)
            {
                if (definitionMap.TryGetValue(lead.MetricDefinitionId, out var leadDef))
                {
                    AppendSimplifiedMetric(sb, lead, leadDef);
                }
            }
        }

        // Constraint metric (guardrail)
        var constraintMetric = entity.GetConstraintMetric();
        if (constraintMetric != null && definitionMap.TryGetValue(constraintMetric.MetricDefinitionId, out var constraintDef))
        {
            sb.AppendLine();
            sb.AppendLine("Constraint Metric (Guardrail - must maintain):");
            AppendSimplifiedMetric(sb, constraintMetric, constraintDef);
        }
    }

    /// <summary>
    /// Simplified metric output: name, description, direction, and target only.
    /// Excludes: data type, unit, aggregation, evaluation window, weight, source hint, baseline, drift threshold.
    /// </summary>
    private static void AppendSimplifiedMetric(StringBuilder sb, GoalMetric goalMetric, MetricDefinition definition)
    {
        var direction = FormatDirection(definition.Direction);
        sb.AppendLine($"  - {definition.Name} ({direction})");

        if (!string.IsNullOrWhiteSpace(definition.Description))
        {
            sb.AppendLine($"    {definition.Description}");
        }

        sb.AppendLine($"    Target: {goalMetric.Target}");
    }

    private static string FormatDirection(MetricDirection direction) => direction switch
    {
        MetricDirection.Increase => "higher is better",
        MetricDirection.Decrease => "lower is better",
        MetricDirection.Maintain => "maintain within range",
        _ => EmbeddingFormatHelper.FormatEnum(direction)
    };
}

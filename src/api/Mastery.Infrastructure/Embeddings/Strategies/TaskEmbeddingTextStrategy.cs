using System.Text;
using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Metrics;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;
using TaskStatus = Mastery.Domain.Enums.TaskStatus;
using Task = Mastery.Domain.Entities.Task.Task;

namespace Mastery.Infrastructure.Embeddings.Strategies;

/// <summary>
/// Embedding text strategy for Task entities.
/// Context depth: Self + Project title + Goal details + User Roles/Values + Dependencies + Metric bindings.
/// </summary>
public sealed class TaskEmbeddingTextStrategy(
    IProjectRepository _projectRepository,
    IGoalRepository _goalRepository,
    IUserProfileRepository _userProfileRepository,
    IMetricDefinitionRepository _metricDefinitionRepository,
    ITaskRepository _taskRepository) : IEmbeddingTextStrategy<Task>
{
    public async Task<string?> CompileTextAsync(Task entity, CancellationToken ct)
    {
        // Don't embed archived tasks
        if (entity.Status == TaskStatus.Archived)
        {
            return null;
        }

        var sb = new StringBuilder();

        // Build leading summary: "{Title} - {EstimatedMinutes}min {EnergyCost} energy task"
        var energyText = FormatEnergyCostShort(entity.EnergyCost);
        var blockedIndicator = entity.HasDependencies ? " (BLOCKED)" : "";
        EmbeddingFormatHelper.AppendSummary(sb, "TASK",
            $"{entity.Title} - {entity.EstimatedMinutes}min {energyText} energy task{blockedIndicator}");

        // Basic task information
        sb.AppendLine($"Title: {entity.Title}");
        EmbeddingFormatHelper.AppendFieldIfNotEmpty(sb, "Description", entity.Description);

        sb.AppendLine($"Status: {FormatTaskStatus(entity.Status)}");
        sb.AppendLine($"Priority: {EmbeddingFormatHelper.FormatPriority(entity.Priority)}");
        sb.AppendLine($"Estimated time: {entity.EstimatedMinutes} minutes");
        sb.AppendLine($"Energy cost: {FormatEnergyCost(entity.EnergyCost)}");

        // Include due date if present
        if (entity.Due != null && entity.Due.DueOn.HasValue)
        {
            var dueType = entity.Due.DueType == DueType.Hard ? " (HARD deadline)" : " (soft deadline)";
            sb.AppendLine($"Due: {EmbeddingFormatHelper.FormatDate(entity.Due.DueOn.Value)}{dueType}");
        }

        // Include scheduling if present
        if (entity.Scheduling != null)
        {
            sb.AppendLine($"Scheduled for: {EmbeddingFormatHelper.FormatDate(entity.Scheduling.ScheduledOn)}");
        }

        // Include parent project context
        if (entity.ProjectId.HasValue)
        {
            var project = await _projectRepository.GetByIdAsync(entity.ProjectId.Value, ct);
            if (project != null)
            {
                sb.AppendLine();
                sb.AppendLine($"Project: {project.Title}");
                EmbeddingFormatHelper.AppendFieldIfNotEmpty(sb, "Project description", project.Description);
            }
        }

        // Include parent goal context
        if (entity.GoalId.HasValue)
        {
            var goal = await _goalRepository.GetByIdAsync(entity.GoalId.Value, ct);
            if (goal != null)
            {
                sb.AppendLine();
                sb.AppendLine($"Goal: {goal.Title}");
                EmbeddingFormatHelper.AppendFieldIfNotEmpty(sb, "Goal why", goal.Why);
            }
        }

        // Include context tags (formatted)
        if (entity.ContextTags.Count > 0)
        {
            var tags = entity.ContextTags.Select(FormatContextTag).ToList();
            sb.AppendLine($"Context: {string.Join(", ", tags)}");
        }

        // Include user roles and values (simplified)
        await AppendUserContextAsync(sb, entity, ct);

        // Include dependencies with titles
        await AppendDependenciesAsync(sb, entity, ct);

        // Include metric bindings (simplified)
        await AppendMetricBindingsAsync(sb, entity, ct);

        // Include reschedule history if meaningful
        if (entity.RescheduleCount > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"Rescheduled: {entity.RescheduleCount} time(s)");
            if (entity.LastRescheduleReason.HasValue)
            {
                sb.AppendLine($"Last reason: {FormatRescheduleReason(entity.LastRescheduleReason.Value)}");
            }
        }

        // Domain keywords for semantic search
        EmbeddingFormatHelper.AppendKeywords(sb,
            "task", "action", "todo", "work", "blocked", "dependency",
            "due date", "energy", "context", "next action", "deliverable");

        return sb.ToString();
    }

    private async System.Threading.Tasks.Task AppendUserContextAsync(StringBuilder sb, Task entity, CancellationToken ct)
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

    private async System.Threading.Tasks.Task AppendDependenciesAsync(StringBuilder sb, Task entity, CancellationToken ct)
    {
        if (!entity.HasDependencies) return;

        var dependencyTasks = new List<Task>();
        foreach (var depId in entity.DependencyTaskIds)
        {
            var depTask = await _taskRepository.GetByIdAsync(depId, ct);
            if (depTask != null)
            {
                dependencyTasks.Add(depTask);
            }
        }

        if (dependencyTasks.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("BLOCKED BY:");
            foreach (var dep in dependencyTasks)
            {
                sb.AppendLine($"  - {dep.Title} ({FormatTaskStatus(dep.Status)})");
            }
        }
    }

    private async System.Threading.Tasks.Task AppendMetricBindingsAsync(StringBuilder sb, Task entity, CancellationToken ct)
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

    private static string FormatTaskStatus(TaskStatus status) => status switch
    {
        TaskStatus.Inbox => "Inbox (not yet processed)",
        TaskStatus.Ready => "Ready (can be started)",
        TaskStatus.Scheduled => "Scheduled (assigned to a date)",
        TaskStatus.InProgress => "In Progress",
        TaskStatus.Completed => "Completed",
        TaskStatus.Cancelled => "Cancelled",
        TaskStatus.Archived => "Archived",
        _ => EmbeddingFormatHelper.FormatEnum(status)
    };

    private static string FormatEnergyCost(int cost) => cost switch
    {
        1 => "1/5 (Very Low - can do anytime)",
        2 => "2/5 (Low)",
        3 => "3/5 (Medium)",
        4 => "4/5 (High - needs focus)",
        5 => "5/5 (Very High - peak energy required)",
        _ => $"{cost}/5"
    };

    private static string FormatEnergyCostShort(int cost) => cost switch
    {
        1 => "very low",
        2 => "low",
        3 => "medium",
        4 => "high",
        5 => "very high",
        _ => $"{cost}/5"
    };

    private static string FormatContextTag(ContextTag tag) => tag switch
    {
        ContextTag.Computer => "Computer",
        ContextTag.Phone => "Phone",
        ContextTag.Errands => "Errands (requires going out)",
        ContextTag.Home => "Home",
        ContextTag.Office => "Office",
        ContextTag.DeepWork => "Deep Work (focus required)",
        ContextTag.LowEnergy => "Low Energy (can do when tired)",
        ContextTag.Anywhere => "Anywhere",
        _ => EmbeddingFormatHelper.FormatEnum(tag)
    };

    private static string FormatRescheduleReason(RescheduleReason reason) => reason switch
    {
        RescheduleReason.NoTime => "No time available",
        RescheduleReason.TooTired => "Too tired (energy too low)",
        RescheduleReason.Blocked => "Blocked by dependency",
        RescheduleReason.Forgot => "Forgot about it",
        RescheduleReason.ScopeTooBig => "Scope too big for available time",
        RescheduleReason.WaitingOnSomeone => "Waiting on someone else",
        RescheduleReason.Other => "Other reason",
        _ => EmbeddingFormatHelper.FormatEnum(reason)
    };

    private static string FormatContributionType(TaskContributionType type) => type switch
    {
        TaskContributionType.BooleanAs1 => "adds 1 on completion",
        TaskContributionType.FixedValue => "adds fixed value",
        TaskContributionType.UseActualMinutes => "uses actual minutes spent",
        TaskContributionType.UseEnteredValue => "uses entered value",
        _ => EmbeddingFormatHelper.FormatEnum(type)
    };
}

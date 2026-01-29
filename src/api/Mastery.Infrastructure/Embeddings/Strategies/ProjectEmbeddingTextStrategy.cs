using System.Text;
using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Project;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;

namespace Mastery.Infrastructure.Embeddings.Strategies;

/// <summary>
/// Embedding text strategy for Project entities.
/// Context depth: Self + Goal title + Next task.
/// </summary>
public sealed class ProjectEmbeddingTextStrategy(
    IGoalRepository _goalRepository,
    ITaskRepository _taskRepository)
    : IEmbeddingTextStrategy<Project>
{
    public async Task<string?> CompileTextAsync(Project entity, CancellationToken ct)
    {
        var sb = new StringBuilder();

        // Build leading summary: "{Title} - {Status} project with {completed}/{total} milestones"
        var milestonesSummary = entity.HasMilestones
            ? $" with {entity.CompletedMilestonesCount}/{entity.Milestones.Count} milestones completed"
            : "";
        var stuckIndicator = entity.IsStuck ? " (STUCK - needs next action)" : "";
        EmbeddingFormatHelper.AppendSummary(sb, "PROJECT",
            $"{entity.Title} - {FormatProjectStatus(entity.Status)} project{milestonesSummary}{stuckIndicator}");

        // Basic info
        sb.AppendLine($"Title: {entity.Title}");
        EmbeddingFormatHelper.AppendFieldIfNotEmpty(sb, "Description", entity.Description);

        sb.AppendLine($"Status: {FormatProjectStatus(entity.Status)}");
        sb.AppendLine($"Priority: {EmbeddingFormatHelper.FormatPriority(entity.Priority)}");
        EmbeddingFormatHelper.AppendDateFieldIfPresent(sb, "Target end date", entity.TargetEndDate);

        // Include parent goal context
        if (entity.GoalId.HasValue)
        {
            var goal = await _goalRepository.GetByIdAsync(entity.GoalId.Value, ct);
            if (goal != null)
            {
                sb.AppendLine();
                sb.AppendLine($"Parent goal: {goal.Title}");
                EmbeddingFormatHelper.AppendFieldIfNotEmpty(sb, "Goal description", goal.Description);
            }
        }

        // Include next task context
        if (entity.NextTaskId.HasValue)
        {
            var nextTask = await _taskRepository.GetByIdAsync(entity.NextTaskId.Value, ct);
            if (nextTask != null)
            {
                sb.AppendLine();
                sb.AppendLine($"Next action: {nextTask.Title}");
                EmbeddingFormatHelper.AppendFieldIfNotEmpty(sb, "Task description", nextTask.Description);
            }
        }

        // Include milestones summary
        if (entity.HasMilestones)
        {
            sb.AppendLine();
            sb.AppendLine($"Milestones: {entity.CompletedMilestonesCount}/{entity.Milestones.Count} completed");

            var pendingMilestones = entity.Milestones
                .Where(m => m.Status != MilestoneStatus.Completed)
                .OrderBy(m => m.DisplayOrder)
                .Take(3)
                .ToList();

            if (pendingMilestones.Count > 0)
            {
                sb.AppendLine("Upcoming milestones:");
                foreach (var m in pendingMilestones)
                {
                    var dueDate = m.TargetDate.HasValue
                        ? $" (due {EmbeddingFormatHelper.FormatDate(m.TargetDate.Value)})"
                        : "";
                    sb.AppendLine($"  - {m.Title}{dueDate}");
                }
            }
        }

        // Highlight stuck status prominently
        if (entity.IsStuck)
        {
            sb.AppendLine();
            sb.AppendLine("WARNING: Project is STUCK - needs next action defined");
        }

        // Domain keywords for semantic search
        EmbeddingFormatHelper.AppendKeywords(sb,
            "project", "milestone", "deliverable", "deadline", "progress",
            "stuck", "blocked", "next action", "completion", "initiative");

        return sb.ToString();
    }

    private static string FormatProjectStatus(ProjectStatus status) => status switch
    {
        ProjectStatus.Draft => "Draft (not started)",
        ProjectStatus.Active => "Active",
        ProjectStatus.Paused => "Paused",
        ProjectStatus.Completed => "Completed",
        ProjectStatus.Archived => "Archived",
        _ => EmbeddingFormatHelper.FormatEnum(status)
    };
}

using Mastery.Application.Common.Models;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;
using Mastery.Domain.Events;
using TaskStatus = Mastery.Domain.Enums.TaskStatus;

namespace Mastery.Infrastructure.Services.Rules;

/// <summary>
/// Detects projects that have stalled with no task completions in N days.
/// Triggers when: Project has active tasks but none have been completed recently.
/// </summary>
public sealed class ProjectStuckRule : DeterministicRuleBase
{
    private const int CriticalStuckDays = 21;
    private const int HighStuckDays = 14;
    private const int MediumStuckDays = 7;
    private const int CriticalActiveTaskCount = 5;
    private const int HighActiveTaskCount = 3;

    public override string RuleId => "PROJECT_STUCK";
    public override string RuleName => "Project Stuck Detection";
    public override string Description => "Detects projects with no task completions in a sustained period that may need attention.";

    public override Task<RuleResult> EvaluateAsync(
        UserStateSnapshot state,
        IReadOnlyList<SignalEntry> signals,
        CancellationToken ct = default)
    {
        // Early exit if no relevant signals in the batch
        var relevantSignals = signals.Where(s =>
            s.EventType == nameof(TaskCompletedEvent) ||
            s.EventType == nameof(TaskStatusChangedEvent) ||
            s.EventType == nameof(ProjectCreatedEvent) ||
            s.EventType == nameof(ProjectUpdatedEvent) ||
            s.EventType == "MorningWindowStart").ToList();

        if (relevantSignals.Count == 0)
            return Task.FromResult(NotTriggered());

        var stuckProjects = new List<ProjectStuckAnalysis>();

        // Get task completion signals from recent history (use signal batch for recency check)
        var recentTaskCompletions = signals
            .Where(s => s.EventType == nameof(TaskCompletedEvent) && s.TargetEntityId.HasValue)
            .Select(s => s.TargetEntityId!.Value)
            .ToHashSet();

        foreach (var project in state.Projects.Where(p => p.Status == ProjectStatus.Active))
        {
            // Get all tasks for this project
            var projectTasks = state.Tasks
                .Where(t => t.ProjectId == project.Id)
                .ToList();

            if (projectTasks.Count == 0)
                continue;

            // Count active (not completed, not cancelled) tasks
            var activeTasks = projectTasks
                .Where(t => t.Status != TaskStatus.Completed && t.Status != TaskStatus.Cancelled)
                .ToList();

            if (activeTasks.Count == 0)
                continue; // All tasks done or cancelled - project completing normally

            // Check for recent completions using completed task count from project snapshot
            // If project has completed tasks but no recent completions in signal batch, check signal history
            var hasRecentCompletion = projectTasks.Any(t =>
                t.Status == TaskStatus.Completed && recentTaskCompletions.Contains(t.Id));

            // Calculate days since last activity
            // Use the project's completion progress to estimate activity level
            var daysSinceActivity = EstimateDaysSinceActivity(project, projectTasks, signals, state.Today);

            if (daysSinceActivity >= MediumStuckDays)
            {
                stuckProjects.Add(new ProjectStuckAnalysis(
                    Project: project,
                    DaysSinceActivity: daysSinceActivity,
                    ActiveTaskCount: activeTasks.Count,
                    TotalTaskCount: projectTasks.Count,
                    CompletedTaskCount: project.CompletedTasks,
                    HasRecentCompletion: hasRecentCompletion,
                    IsLinkedToGoal: project.GoalId.HasValue));
            }
        }

        if (stuckProjects.Count == 0)
            return Task.FromResult(NotTriggered());

        // Prioritize by: priority, then how stuck (longest days first)
        var mostStuck = stuckProjects
            .OrderBy(p => p.Project.Priority)
            .ThenByDescending(p => p.DaysSinceActivity)
            .First();

        // Check if linked to a high-priority goal
        var linkedGoalPriority = mostStuck.IsLinkedToGoal
            ? state.Goals.FirstOrDefault(g => g.Id == mostStuck.Project.GoalId)?.Priority
            : null;

        var severity = ComputeSeverity(
            mostStuck.DaysSinceActivity,
            mostStuck.ActiveTaskCount,
            mostStuck.Project.Priority,
            linkedGoalPriority);

        var evidence = new Dictionary<string, object>
        {
            ["StuckProjectCount"] = stuckProjects.Count,
            ["MostStuckProjectId"] = mostStuck.Project.Id,
            ["MostStuckProjectTitle"] = mostStuck.Project.Title,
            ["MostStuckPriority"] = mostStuck.Project.Priority,
            ["DaysSinceActivity"] = mostStuck.DaysSinceActivity,
            ["ActiveTaskCount"] = mostStuck.ActiveTaskCount,
            ["TotalTaskCount"] = mostStuck.TotalTaskCount,
            ["CompletedTaskCount"] = mostStuck.CompletedTaskCount,
            ["CompletionPercentage"] = mostStuck.TotalTaskCount > 0
                ? Math.Round((decimal)mostStuck.CompletedTaskCount / mostStuck.TotalTaskCount * 100, 1)
                : 0,
            ["IsLinkedToGoal"] = mostStuck.IsLinkedToGoal,
            ["LinkedGoalPriority"] = linkedGoalPriority ?? (object)"N/A",
            ["AllStuckProjects"] = stuckProjects.Select(p => new
            {
                p.Project.Id,
                p.Project.Title,
                p.Project.Priority,
                p.DaysSinceActivity,
                p.ActiveTaskCount
            }).ToList()
        };

        var title = $"\"{mostStuck.Project.Title}\" has stalled ({mostStuck.DaysSinceActivity} days, {mostStuck.ActiveTaskCount} tasks waiting)";

        var rationale = BuildRationale(mostStuck, linkedGoalPriority);

        var score = ComputeScore(mostStuck.DaysSinceActivity, mostStuck.ActiveTaskCount, mostStuck.Project.Priority);

        var directRecommendation = new DirectRecommendationCandidate(
            Type: RecommendationType.ProjectStuckFix,
            Context: RecommendationContext.DriftAlert,
            TargetKind: RecommendationTargetKind.Project,
            TargetEntityId: mostStuck.Project.Id,
            TargetEntityTitle: mostStuck.Project.Title,
            ActionKind: RecommendationActionKind.ReflectPrompt,
            Title: title,
            Rationale: rationale,
            Score: score,
            ActionSummary: "Review project status and next actions");

        return Task.FromResult(Triggered(severity, evidence, directRecommendation));
    }

    private static int EstimateDaysSinceActivity(
        ProjectSnapshot project,
        List<TaskSnapshot> projectTasks,
        IReadOnlyList<SignalEntry> signals,
        DateOnly today)
    {
        // Look for any recent task signals related to this project
        var projectTaskIds = projectTasks.Select(t => t.Id).ToHashSet();

        var lastTaskSignal = signals
            .Where(s =>
                s.TargetEntityId.HasValue &&
                projectTaskIds.Contains(s.TargetEntityId.Value) &&
                (s.EventType == nameof(TaskCompletedEvent) ||
                 s.EventType == nameof(TaskStatusChangedEvent) ||
                 s.EventType == nameof(TaskCreatedEvent)))
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefault();

        if (lastTaskSignal != null)
        {
            return Math.Max(0, today.DayNumber - DateOnly.FromDateTime(lastTaskSignal.CreatedAt).DayNumber);
        }

        // If no task signals in batch, estimate from project completion rate
        // If project has some completions, assume activity was somewhat recent
        // This is a heuristic - in production, you'd track last activity timestamp
        if (project.CompletedTasks > 0)
        {
            // Assume some progress was made, use a default moderate stuck period
            return MediumStuckDays;
        }

        // No completions and no recent signals - assume stuck for a while
        return HighStuckDays;
    }

    private static RuleSeverity ComputeSeverity(
        int daysSinceActivity,
        int activeTaskCount,
        int projectPriority,
        int? linkedGoalPriority)
    {
        // Critical: 21+ days stuck with 5+ active tasks, OR linked to P1 goal and 14+ days
        if ((daysSinceActivity >= CriticalStuckDays && activeTaskCount >= CriticalActiveTaskCount) ||
            (linkedGoalPriority == 1 && daysSinceActivity >= HighStuckDays))
        {
            return RuleSeverity.Critical;
        }

        // High: 14+ days stuck OR 3+ active tasks stuck for 7+ days OR P1/P2 project
        if (daysSinceActivity >= HighStuckDays ||
            (activeTaskCount >= HighActiveTaskCount && daysSinceActivity >= MediumStuckDays) ||
            (projectPriority <= 2 && daysSinceActivity >= MediumStuckDays))
        {
            return RuleSeverity.High;
        }

        // Medium: 7+ days stuck
        if (daysSinceActivity >= MediumStuckDays)
        {
            return RuleSeverity.Medium;
        }

        return RuleSeverity.Low;
    }

    private static decimal ComputeScore(int daysSinceActivity, int activeTaskCount, int priority)
    {
        // Base score from days stuck
        var baseScore = daysSinceActivity switch
        {
            >= CriticalStuckDays => 0.85m,
            >= HighStuckDays => 0.75m,
            >= MediumStuckDays => 0.65m,
            _ => 0.55m
        };

        // Active task count bonus (more tasks waiting = more urgent)
        var taskBonus = activeTaskCount switch
        {
            >= CriticalActiveTaskCount => 0.10m,
            >= HighActiveTaskCount => 0.05m,
            _ => 0.0m
        };

        // Priority bonus
        var priorityBonus = priority switch
        {
            1 => 0.05m,
            2 => 0.02m,
            _ => 0.0m
        };

        return Math.Min(baseScore + taskBonus + priorityBonus, 0.95m);
    }

    private static string BuildRationale(ProjectStuckAnalysis analysis, int? linkedGoalPriority)
    {
        var progressNote = analysis.TotalTaskCount > 0
            ? $" ({analysis.CompletedTaskCount}/{analysis.TotalTaskCount} tasks completed)"
            : "";

        var goalNote = linkedGoalPriority.HasValue
            ? $" This project is linked to a P{linkedGoalPriority} goal, so the delay may impact your goal progress."
            : "";

        return analysis.DaysSinceActivity switch
        {
            >= CriticalStuckDays =>
                $"This project has had no task completions in {analysis.DaysSinceActivity} days{progressNote}. " +
                $"With {analysis.ActiveTaskCount} tasks still waiting, consider whether the project is still relevant " +
                $"or if there's a blocker to address.{goalNote}",

            >= HighStuckDays =>
                $"No progress on this project in {analysis.DaysSinceActivity} days. " +
                $"{analysis.ActiveTaskCount} tasks are waiting{progressNote}. " +
                $"Review the next task and identify what's blocking progress.{goalNote}",

            _ =>
                $"This project has been quiet for {analysis.DaysSinceActivity} days with {analysis.ActiveTaskCount} tasks pending{progressNote}. " +
                $"Consider scheduling time for the next action.{goalNote}"
        };
    }

    private sealed record ProjectStuckAnalysis(
        ProjectSnapshot Project,
        int DaysSinceActivity,
        int ActiveTaskCount,
        int TotalTaskCount,
        int CompletedTaskCount,
        bool HasRecentCompletion,
        bool IsLinkedToGoal);
}

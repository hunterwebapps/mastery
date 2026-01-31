using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;
using TaskStatus = Mastery.Domain.Enums.TaskStatus;

namespace Mastery.Domain.Diagnostics.Rules;

/// <summary>
/// Detects tasks or projects with deadlines approaching within 24-48 hours
/// that have insufficient progress.
/// </summary>
public sealed class DeadlineProximityRule : DeterministicRuleBase
{
    private const int UrgentHours = 24;
    private const int WarningHours = 48;
    private const decimal WarningProgressThreshold = 0.5m; // 50% required at 48h
    private const decimal UrgentProgressThreshold = 0.75m; // 75% required at 24h
    private const int SeverityItemCountThreshold = 3;

    public override string RuleId => "DEADLINE_PROXIMITY";
    public override string RuleName => "Deadline Proximity Alert";
    public override string Description => "Detects tasks and projects with imminent deadlines and insufficient progress.";

    public override Task<RuleResult> EvaluateAsync(
        UserStateSnapshot state,
        IReadOnlyList<SignalEntry> signals,
        CancellationToken ct = default)
    {
        var urgentItems = new List<(string Type, Guid Id, string Title, int HoursUntil, decimal Progress, bool IsOverdue)>();

        // Check tasks with due dates
        foreach (var task in state.Tasks.Where(t =>
            t.DueDate.HasValue &&
            t.Status != TaskStatus.Completed &&
            t.Status != TaskStatus.Cancelled))
        {
            var dueDate = task.DueDate!.Value;
            var hoursUntil = (dueDate.ToDateTime(TimeOnly.MinValue) - state.Today.ToDateTime(TimeOnly.MinValue)).TotalHours;

            // Include overdue items (hoursUntil <= 0) and items within warning window
            if (hoursUntil <= WarningHours)
            {
                // Tasks are binary: 0% or 100%
                urgentItems.Add(("Task", task.Id, task.Title, (int)hoursUntil, 0m, hoursUntil <= 0));
            }
        }

        // Check projects with target end dates
        foreach (var project in state.Projects.Where(p =>
            p.TargetEndDate.HasValue &&
            p.Status == ProjectStatus.Active))
        {
            var targetEndDate = project.TargetEndDate!.Value;
            var hoursUntil = (targetEndDate.ToDateTime(TimeOnly.MinValue) - state.Today.ToDateTime(TimeOnly.MinValue)).TotalHours;

            // Include overdue items and items within warning window
            if (hoursUntil <= WarningHours)
            {
                var progress = project.TotalTasks > 0
                    ? (decimal)project.CompletedTasks / project.TotalTasks
                    : 0m;

                // Sliding scale: require more progress as deadline approaches
                var requiredProgress = hoursUntil <= UrgentHours
                    ? UrgentProgressThreshold
                    : WarningProgressThreshold;

                // Overdue items always qualify; otherwise check progress threshold
                if (hoursUntil <= 0 || progress < requiredProgress)
                {
                    urgentItems.Add(("Project", project.Id, project.Title, (int)hoursUntil, progress, hoursUntil <= 0));
                }
            }
        }

        // Check goals with deadlines
        foreach (var goal in state.Goals.Where(g =>
            g.Deadline.HasValue &&
            g.Status == GoalStatus.Active))
        {
            var deadline = goal.Deadline!.Value.ToDateTime(TimeOnly.MinValue);
            var today = state.Today.ToDateTime(TimeOnly.MinValue);
            var hoursUntil = (deadline - today).TotalHours;

            // Include overdue items and items within warning window
            if (hoursUntil <= WarningHours)
            {
                // Calculate goal progress from metrics
                var progress = CalculateGoalProgress(goal);

                // Sliding scale: require more progress as deadline approaches
                var requiredProgress = hoursUntil <= UrgentHours
                    ? UrgentProgressThreshold
                    : WarningProgressThreshold;

                // Overdue items always qualify; otherwise check progress threshold
                if (hoursUntil <= 0 || progress < requiredProgress)
                {
                    urgentItems.Add(("Goal", goal.Id, goal.Title, (int)hoursUntil, progress, hoursUntil <= 0));
                }
            }
        }

        if (urgentItems.Count == 0)
            return Task.FromResult(NotTriggered());

        var mostUrgent = urgentItems.MinBy(i => i.HoursUntil)!;
        var overdueCount = urgentItems.Count(i => i.IsOverdue);

        // Severity is Critical if: any item is overdue, any item due within 24h, or 3+ urgent items
        var severity = mostUrgent.HoursUntil <= UrgentHours ||
                       overdueCount > 0 ||
                       urgentItems.Count >= SeverityItemCountThreshold
            ? RuleSeverity.Critical
            : RuleSeverity.High;

        var evidence = new Dictionary<string, object>
        {
            ["UrgentItemCount"] = urgentItems.Count,
            ["OverdueCount"] = overdueCount,
            ["MostUrgentType"] = mostUrgent.Type,
            ["MostUrgentId"] = mostUrgent.Id,
            ["MostUrgentTitle"] = mostUrgent.Title,
            ["MostUrgentHoursUntil"] = mostUrgent.HoursUntil,
            ["MostUrgentIsOverdue"] = mostUrgent.IsOverdue,
            ["MostUrgentProgress"] = Math.Round(mostUrgent.Progress * 100, 1),
            ["AllUrgentItems"] = urgentItems.Select(i => new { i.Type, i.Id, i.Title, i.HoursUntil, i.IsOverdue }).ToList()
        };

        // Create direct recommendation for the most urgent item
        var targetKind = mostUrgent.Type switch
        {
            "Task" => RecommendationTargetKind.Task,
            "Project" => RecommendationTargetKind.Project,
            "Goal" => RecommendationTargetKind.Goal,
            _ => RecommendationTargetKind.UserProfile
        };

        var (title, rationale) = mostUrgent.IsOverdue
            ? ($"Overdue: \"{mostUrgent.Title}\" was due {Math.Abs(mostUrgent.HoursUntil)} hours ago",
               $"This {mostUrgent.Type.ToLower()} is past its deadline with only {Math.Round(mostUrgent.Progress * 100)}% progress. Address this immediately or reschedule.")
            : ($"Urgent: \"{mostUrgent.Title}\" due in {mostUrgent.HoursUntil} hours",
               $"This {mostUrgent.Type.ToLower()} is due soon with only {Math.Round(mostUrgent.Progress * 100)}% progress. Focus on this today to avoid missing the deadline.");

        var directRecommendation = new DirectRecommendationCandidate(
            Type: RecommendationType.NextBestAction,
            Context: RecommendationContext.DriftAlert,
            TargetKind: targetKind,
            TargetEntityId: mostUrgent.Id,
            TargetEntityTitle: mostUrgent.Title,
            ActionKind: RecommendationActionKind.ExecuteToday,
            Title: title,
            Rationale: rationale,
            Score: mostUrgent.IsOverdue ? 0.98m : 0.95m,
            ActionSummary: $"Prioritize {mostUrgent.Type.ToLower()} completion");

        return Task.FromResult(Triggered(severity, evidence, directRecommendation));
    }

    private static decimal CalculateGoalProgress(GoalSnapshot goal)
    {
        if (goal.Metrics.Count == 0)
            return 0m;

        var totalProgress = 0m;
        var totalWeight = 0m;

        foreach (var metric in goal.Metrics)
        {
            if (!metric.CurrentValue.HasValue || metric.TargetValue == 0)
                continue;

            var progress = Math.Min(metric.CurrentValue.Value / metric.TargetValue, 1m);
            totalProgress += progress * metric.Weight;
            totalWeight += metric.Weight;
        }

        return totalWeight > 0 ? totalProgress / totalWeight : 0m;
    }
}

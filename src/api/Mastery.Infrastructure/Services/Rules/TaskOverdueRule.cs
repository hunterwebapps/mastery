using Mastery.Application.Common.Models;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;
using TaskStatus = Mastery.Domain.Enums.TaskStatus;

namespace Mastery.Infrastructure.Services.Rules;

/// <summary>
/// Detects tasks that are past their due date.
/// Suggests action based on how overdue and how often rescheduled.
/// </summary>
public sealed class TaskOverdueRule : DeterministicRuleBase
{
    private const int RescheduleWarningThreshold = 2;
    private const int OverdueDaysCritical = 7;
    private const int RescheduleAbandonmentThreshold = 4;
    private const int OverdueCountCriticalThreshold = 5;
    private const int DefaultTaskEstimateMinutes = 30;

    public override string RuleId => "TASK_OVERDUE";
    public override string RuleName => "Overdue Task Detection";
    public override string Description => "Detects tasks past their due date that need attention or archival.";

    public override Task<RuleResult> EvaluateAsync(
        UserStateSnapshot state,
        IReadOnlyList<SignalEntry> signals,
        CancellationToken ct = default)
    {
        var overdueTasks = state.Tasks
            .Where(t =>
                t.DueDate.HasValue &&
                t.DueDate.Value < state.Today &&
                t.Status != TaskStatus.Completed &&
                t.Status != TaskStatus.Cancelled)
            .Select(t => new
            {
                Task = t,
                DaysOverdue = state.Today.DayNumber - t.DueDate!.Value.DayNumber
            })
            .OrderByDescending(x => x.DaysOverdue)
            .ThenByDescending(x => x.Task.RescheduleCount)
            .ToList();

        if (overdueTasks.Count == 0)
            return Task.FromResult(NotTriggered());

        var mostOverdue = overdueTasks.First();

        // Determine severity based on days overdue, reschedule history, and count
        var severity = (mostOverdue.DaysOverdue, mostOverdue.Task.RescheduleCount, overdueTasks.Count) switch
        {
            (_, _, >= OverdueCountCriticalThreshold) => RuleSeverity.Critical,
            ( >= OverdueDaysCritical, >= RescheduleWarningThreshold, _) => RuleSeverity.Critical,
            ( >= OverdueDaysCritical, _, _) => RuleSeverity.High,
            (_, >= RescheduleWarningThreshold, _) => RuleSeverity.High,
            (_, _, >= 3) => RuleSeverity.Medium,
            ( >= 3, _, _) => RuleSeverity.Medium,
            _ => RuleSeverity.Low
        };

        var evidence = new Dictionary<string, object>
        {
            ["OverdueTaskCount"] = overdueTasks.Count,
            ["MostOverdueId"] = mostOverdue.Task.Id,
            ["MostOverdueTitle"] = mostOverdue.Task.Title,
            ["DaysOverdue"] = mostOverdue.DaysOverdue,
            ["RescheduleCount"] = mostOverdue.Task.RescheduleCount,
            ["OriginalDueDate"] = mostOverdue.Task.DueDate!.Value.ToString("yyyy-MM-dd"),
            ["TotalOverdueMinutes"] = overdueTasks.Sum(t => t.Task.EstMinutes ?? DefaultTaskEstimateMinutes),
            ["AllOverdueTasks"] = overdueTasks.Select(t => new
            {
                t.Task.Id,
                t.Task.Title,
                t.DaysOverdue,
                t.Task.RescheduleCount
            }).ToList()
        };

        // Check if we should generate a batch triage recommendation
        if (overdueTasks.Count >= OverdueCountCriticalThreshold)
        {
            var triageRecommendation = new DirectRecommendationCandidate(
                Type: RecommendationType.TaskTriageSuggestion,
                Context: RecommendationContext.DriftAlert,
                TargetKind: RecommendationTargetKind.TaskList,
                TargetEntityId: null,
                TargetEntityTitle: $"{overdueTasks.Count} overdue tasks",
                ActionKind: RecommendationActionKind.Review,
                Title: $"Triage {overdueTasks.Count} overdue tasks",
                Rationale: $"You have {overdueTasks.Count} overdue tasks totaling ~{overdueTasks.Sum(t => t.Task.EstMinutes ?? DefaultTaskEstimateMinutes)} minutes. This backlog needs attention—consider a dedicated triage session to archive, delegate, reschedule, or commit to each task.",
                Score: 0.9m,
                ActionSummary: "Review and triage overdue backlog");

            // Escalate to Tier 2 for LLM-guided triage when many tasks are overdue
            return Task.FromResult(Triggered(severity, evidence, triageRecommendation, requiresEscalation: true));
        }

        // Decide recommendation type based on history
        // Archive if: (chronic rescheduler AND critically overdue) OR abandoned (rescheduled too many times)
        var shouldSuggestArchive =
            (mostOverdue.Task.RescheduleCount >= RescheduleWarningThreshold && mostOverdue.DaysOverdue >= OverdueDaysCritical)
            || mostOverdue.Task.RescheduleCount >= RescheduleAbandonmentThreshold;

        DirectRecommendationCandidate directRecommendation;

        if (shouldSuggestArchive)
        {
            var archiveRationale = mostOverdue.Task.RescheduleCount >= RescheduleAbandonmentThreshold
                ? $"This task has been rescheduled {mostOverdue.Task.RescheduleCount} times, suggesting it may no longer be a priority. Archive it to clear mental clutter, or if it's truly important, block time now and commit."
                : $"This task is {mostOverdue.DaysOverdue} days overdue and has been rescheduled {mostOverdue.Task.RescheduleCount} times. If it's no longer relevant, archiving it will clear mental clutter. If it is important, consider why it keeps slipping.";

            directRecommendation = new DirectRecommendationCandidate(
                Type: RecommendationType.TaskArchiveSuggestion,
                Context: RecommendationContext.DriftAlert,
                TargetKind: RecommendationTargetKind.Task,
                TargetEntityId: mostOverdue.Task.Id,
                TargetEntityTitle: mostOverdue.Task.Title,
                ActionKind: RecommendationActionKind.Remove,
                Title: $"Consider archiving \"{mostOverdue.Task.Title}\"",
                Rationale: archiveRationale,
                Score: 0.8m,
                ActionSummary: "Archive or recommit to task");
        }
        else
        {
            var rescheduleRationale = mostOverdue.Task.RescheduleCount > 0
                ? $"This task has been rescheduled {mostOverdue.Task.RescheduleCount} time(s). Consider breaking it down or addressing what's blocking progress."
                : "Set a new due date or schedule time today to complete this task.";

            directRecommendation = new DirectRecommendationCandidate(
                Type: RecommendationType.ScheduleAdjustmentSuggestion,
                Context: RecommendationContext.DriftAlert,
                TargetKind: RecommendationTargetKind.Task,
                TargetEntityId: mostOverdue.Task.Id,
                TargetEntityTitle: mostOverdue.Task.Title,
                ActionKind: RecommendationActionKind.Update,
                Title: $"\"{mostOverdue.Task.Title}\" is {mostOverdue.DaysOverdue} days overdue",
                Rationale: rescheduleRationale,
                Score: 0.75m,
                ActionSummary: "Reschedule or break down task");
        }

        return Task.FromResult(Triggered(severity, evidence, directRecommendation));
    }
}

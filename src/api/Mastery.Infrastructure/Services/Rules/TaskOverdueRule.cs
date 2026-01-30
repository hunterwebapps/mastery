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

        // Determine severity based on days overdue and reschedule history
        var severity = (mostOverdue.DaysOverdue, mostOverdue.Task.RescheduleCount) switch
        {
            ( >= OverdueDaysCritical, >= RescheduleWarningThreshold) => RuleSeverity.Critical,
            ( >= OverdueDaysCritical, _) => RuleSeverity.High,
            (_, >= RescheduleWarningThreshold) => RuleSeverity.High,
            ( >= 3, _) => RuleSeverity.Medium,
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
            ["TotalOverdueMinutes"] = overdueTasks.Sum(t => t.Task.EstMinutes ?? 30),
            ["AllOverdueTasks"] = overdueTasks.Select(t => new
            {
                t.Task.Id,
                t.Task.Title,
                t.DaysOverdue,
                t.Task.RescheduleCount
            }).ToList()
        };

        // Decide recommendation type based on history
        var shouldSuggestArchive = mostOverdue.Task.RescheduleCount >= RescheduleWarningThreshold
                                   && mostOverdue.DaysOverdue >= OverdueDaysCritical;

        DirectRecommendationCandidate directRecommendation;

        if (shouldSuggestArchive)
        {
            directRecommendation = new DirectRecommendationCandidate(
                Type: RecommendationType.TaskArchiveSuggestion,
                Context: RecommendationContext.DriftAlert,
                TargetKind: RecommendationTargetKind.Task,
                TargetEntityId: mostOverdue.Task.Id,
                TargetEntityTitle: mostOverdue.Task.Title,
                ActionKind: RecommendationActionKind.Remove,
                Title: $"Consider archiving \"{mostOverdue.Task.Title}\"",
                Rationale: $"This task is {mostOverdue.DaysOverdue} days overdue and has been rescheduled {mostOverdue.Task.RescheduleCount} times. If it's no longer relevant, archiving it will clear mental clutter. If it is important, consider why it keeps slipping.",
                Score: 0.7m,
                ActionSummary: "Archive or recommit to task");
        }
        else
        {
            directRecommendation = new DirectRecommendationCandidate(
                Type: RecommendationType.ScheduleAdjustmentSuggestion,
                Context: RecommendationContext.DriftAlert,
                TargetKind: RecommendationTargetKind.Task,
                TargetEntityId: mostOverdue.Task.Id,
                TargetEntityTitle: mostOverdue.Task.Title,
                ActionKind: RecommendationActionKind.Update,
                Title: $"\"{mostOverdue.Task.Title}\" is {mostOverdue.DaysOverdue} days overdue",
                Rationale: mostOverdue.Task.RescheduleCount > 0
                    ? $"This task has been rescheduled {mostOverdue.Task.RescheduleCount} time(s). Consider breaking it down or addressing what's blocking progress."
                    : "Set a new due date or schedule time today to complete this task.",
                Score: 0.75m,
                ActionSummary: "Reschedule or break down task");
        }

        return Task.FromResult(Triggered(severity, evidence, directRecommendation));
    }
}

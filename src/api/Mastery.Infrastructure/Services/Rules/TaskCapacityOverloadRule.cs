using Mastery.Application.Common.Models;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;
using TaskStatus = Mastery.Domain.Enums.TaskStatus;

namespace Mastery.Infrastructure.Services.Rules;

/// <summary>
/// Detects when planned work exceeds available capacity by more than 20%.
/// Triggers when: sum(scheduled task minutes) > capacity × 1.2
/// </summary>
public sealed class TaskCapacityOverloadRule : DeterministicRuleBase
{
    private const decimal OverloadThreshold = 1.2m;

    public override string RuleId => "TASK_CAPACITY_OVERLOAD";
    public override string RuleName => "Capacity Overload Detection";
    public override string Description => "Detects when planned work for today exceeds available capacity by more than 20%.";

    public override Task<RuleResult> EvaluateAsync(
        UserStateSnapshot state,
        IReadOnlyList<SignalEntry> signals,
        CancellationToken ct = default)
    {
        if (state.Profile?.Constraints == null)
            return Task.FromResult(NotTriggered());

        var constraints = state.Profile.Constraints;
        var isWeekend = state.Today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        var capacityMinutes = isWeekend
            ? constraints.MaxPlannedMinutesWeekend
            : constraints.MaxPlannedMinutesWeekday;

        // Sum up all tasks scheduled for today that are not completed
        var todayTasks = state.Tasks
            .Where(t => t.ScheduledDate == state.Today &&
                       t.Status != TaskStatus.Completed &&
                       t.Status != TaskStatus.Cancelled)
            .ToList();

        var plannedMinutes = todayTasks.Sum(t => t.EstMinutes ?? 30); // Default 30 min if not specified

        var threshold = (int)(capacityMinutes * OverloadThreshold);

        if (plannedMinutes <= threshold)
            return Task.FromResult(NotTriggered());

        var overloadPercentage = (decimal)plannedMinutes / capacityMinutes * 100 - 100;
        var severity = overloadPercentage switch
        {
            > 50 => RuleSeverity.Critical,
            > 30 => RuleSeverity.High,
            > 20 => RuleSeverity.Medium,
            _ => RuleSeverity.Low
        };

        var evidence = new Dictionary<string, object>
        {
            ["PlannedMinutes"] = plannedMinutes,
            ["CapacityMinutes"] = capacityMinutes,
            ["OverloadPercentage"] = Math.Round(overloadPercentage, 1),
            ["TaskCount"] = todayTasks.Count,
            ["IsWeekend"] = isWeekend
        };

        // Create direct recommendation to defer non-critical tasks
        var directRecommendation = new DirectRecommendationCandidate(
            Type: RecommendationType.PlanRealismAdjustment,
            Context: RecommendationContext.DriftAlert,
            TargetKind: RecommendationTargetKind.UserProfile,
            TargetEntityId: null,
            TargetEntityTitle: null,
            ActionKind: RecommendationActionKind.ReflectPrompt,
            Title: $"Today's plan is {Math.Round(overloadPercentage)}% over capacity",
            Rationale: $"You have {plannedMinutes} minutes of tasks scheduled but only {capacityMinutes} minutes of available capacity. Consider deferring {todayTasks.Count - (int)(capacityMinutes / 30)} tasks to reduce stress and improve completion rate.",
            Score: 0.9m,
            ActionSummary: "Review and reschedule lower-priority tasks");

        return Task.FromResult(Triggered(severity, evidence, directRecommendation));
    }
}

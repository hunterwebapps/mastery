using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;
using TaskStatus = Mastery.Domain.Enums.TaskStatus;

namespace Mastery.Domain.Diagnostics.Rules;

/// <summary>
/// Detects when high-energy tasks are scheduled on days with reported low energy.
/// Triggers when: user reported low energy in morning check-in AND has high-energy tasks scheduled.
///
/// Optimizations:
/// - Respects task priority and deadline proximity (won't recommend deferring critical deadline tasks)
/// - Multi-factor severity calculation using capacity burden ratio
/// - Dynamic recommendation score based on severity
/// </summary>
public sealed class TaskEnergyMismatchRule : DeterministicRuleBase
{
    private const int LowEnergyThreshold = 3;  // Energy levels 1-3 considered low
    private const int HighEnergyTaskThreshold = 4; // Task energy requirement 4-5 is high
    private const int HighPriorityThreshold = 3; // Priority 3+ considered high
    private const int DefaultCapacityMinutes = 480; // 8 hours default daily capacity

    public override string RuleId => "TASK_ENERGY_MISMATCH";
    public override string RuleName => "Energy Mismatch Detection";
    public override string Description => "Detects when high-energy tasks are scheduled on low-energy days.";

    public override Task<RuleResult> EvaluateAsync(
        UserStateSnapshot state,
        IReadOnlyList<SignalEntry> signals,
        CancellationToken ct = default)
    {
        // Find today's morning check-in
        var todayCheckIn = state.RecentCheckIns
            .FirstOrDefault(c => c.Date == state.Today && c.Type == CheckInType.Morning);

        if (todayCheckIn?.EnergyLevel == null || todayCheckIn.EnergyLevel > LowEnergyThreshold)
            return Task.FromResult(NotTriggered());

        var reportedEnergy = todayCheckIn.EnergyLevel.Value;

        // Find high-energy tasks scheduled for today
        var highEnergyTasks = state.Tasks
            .Where(t =>
                t.ScheduledDate == state.Today &&
                t.Status != TaskStatus.Completed &&
                t.Status != TaskStatus.Cancelled &&
                t.EnergyLevel >= HighEnergyTaskThreshold)
            .ToList();

        if (highEnergyTasks.Count == 0)
            return Task.FromResult(NotTriggered());

        // Partition tasks: critical deadline tasks (due today/tomorrow + high priority) vs deferrable
        var imminentDeadline = state.Today.AddDays(1);
        var criticalTasks = highEnergyTasks
            .Where(t => t.DueDate.HasValue &&
                        t.DueDate.Value <= imminentDeadline &&
                        t.Priority >= HighPriorityThreshold)
            .ToList();

        var deferrableTasks = highEnergyTasks
            .Where(t => !t.DueDate.HasValue ||
                        t.DueDate.Value > imminentDeadline ||
                        t.Priority < HighPriorityThreshold)
            .ToList();

        // If ALL high-energy tasks are critical, don't recommend deferring
        if (deferrableTasks.Count == 0)
            return Task.FromResult(NotTriggered());

        var totalHighEnergyMinutes = highEnergyTasks.Sum(t => t.EstMinutes ?? 30);

        // Multi-factor severity calculation
        var capacityMinutes = state.Profile?.Constraints?.MaxPlannedMinutesWeekday ?? DefaultCapacityMinutes;
        var burdenRatio = (decimal)totalHighEnergyMinutes / capacityMinutes;
        var hasHighPriorityTasks = highEnergyTasks.Any(t => t.Priority >= HighPriorityThreshold);

        var severity = (reportedEnergy, burdenRatio, hasHighPriorityTasks) switch
        {
            (1, > 0.5m, _) => RuleSeverity.Critical,
            (1, > 0.3m, _) => RuleSeverity.High,
            (1, _, _) => RuleSeverity.High,
            (2, > 0.5m, true) => RuleSeverity.High,
            (2, _, _) => RuleSeverity.Medium,
            (3, > 0.5m, _) => RuleSeverity.Medium,
            _ => RuleSeverity.Low
        };

        // Dynamic score based on severity
        var score = severity switch
        {
            RuleSeverity.Critical => 0.95m,
            RuleSeverity.High => 0.85m,
            RuleSeverity.Medium => 0.75m,
            _ => 0.65m
        };

        var evidence = new Dictionary<string, object>
        {
            ["ReportedEnergyLevel"] = reportedEnergy,
            ["HighEnergyTaskCount"] = highEnergyTasks.Count,
            ["TotalHighEnergyMinutes"] = totalHighEnergyMinutes,
            ["CapacityMinutes"] = capacityMinutes,
            ["BurdenRatio"] = Math.Round(burdenRatio, 2),
            ["CriticalDeadlineCount"] = criticalTasks.Count,
            ["DeferrableTaskCount"] = deferrableTasks.Count,
            ["HighEnergyTasks"] = highEnergyTasks.Select(t => new
            {
                t.Id,
                t.Title,
                t.EnergyLevel,
                t.Priority,
                t.DueDate,
                EstMinutes = t.EstMinutes ?? 30,
                IsCritical = criticalTasks.Any(c => c.Id == t.Id)
            }).ToList()
        };

        // Select the best task to defer (from deferrable tasks only)
        var taskToDefer = deferrableTasks
            .OrderByDescending(t => t.EnergyLevel)
            .ThenByDescending(t => t.EstMinutes ?? 30)
            .First();

        var rationale = criticalTasks.Count > 0
            ? $"You reported low energy ({reportedEnergy}/5) today, but have {highEnergyTasks.Count} high-energy tasks scheduled ({totalHighEnergyMinutes} min total). {criticalTasks.Count} task(s) have critical deadlines and should stay. Consider deferring \"{taskToDefer.Title}\" to preserve energy for what matters most."
            : $"You reported low energy ({reportedEnergy}/5) today, but have {highEnergyTasks.Count} high-energy tasks scheduled ({totalHighEnergyMinutes} min total). Consider deferring demanding tasks to preserve what energy you have for essentials.";

        var directRecommendation = new DirectRecommendationCandidate(
            Type: RecommendationType.ScheduleAdjustmentSuggestion,
            Context: RecommendationContext.MorningCheckIn,
            TargetKind: RecommendationTargetKind.Task,
            TargetEntityId: taskToDefer.Id,
            TargetEntityTitle: taskToDefer.Title,
            ActionKind: RecommendationActionKind.Defer,
            Title: $"Consider rescheduling \"{taskToDefer.Title}\" (energy level {reportedEnergy}/5)",
            Rationale: rationale,
            Score: score,
            ActionSummary: "Defer to a higher-energy day");

        return Task.FromResult(Triggered(severity, evidence, directRecommendation));
    }
}

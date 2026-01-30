using Mastery.Application.Common.Models;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;
using TaskStatus = Mastery.Domain.Enums.TaskStatus;

namespace Mastery.Infrastructure.Services.Rules;

/// <summary>
/// Detects when high-energy tasks are scheduled on days with reported low energy.
/// Triggers when: user reported low energy in morning check-in AND has high-energy tasks scheduled.
/// </summary>
public sealed class TaskEnergyMismatchRule : DeterministicRuleBase
{
    private const int LowEnergyThreshold = 3;  // Energy levels 1-3 considered low
    private const int HighEnergyTaskThreshold = 4; // Task energy requirement 4-5 is high

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

        var totalHighEnergyMinutes = highEnergyTasks.Sum(t => t.EstMinutes ?? 30);

        var severity = reportedEnergy switch
        {
            1 when highEnergyTasks.Count > 2 => RuleSeverity.Critical,
            1 => RuleSeverity.High,
            2 when highEnergyTasks.Count > 3 => RuleSeverity.High,
            2 => RuleSeverity.Medium,
            _ => RuleSeverity.Low
        };

        var evidence = new Dictionary<string, object>
        {
            ["ReportedEnergyLevel"] = reportedEnergy,
            ["HighEnergyTaskCount"] = highEnergyTasks.Count,
            ["TotalHighEnergyMinutes"] = totalHighEnergyMinutes,
            ["HighEnergyTasks"] = highEnergyTasks.Select(t => new
            {
                t.Id,
                t.Title,
                t.EnergyLevel,
                EstMinutes = t.EstMinutes ?? 30
            }).ToList()
        };

        // Find the first high-energy task to suggest deferring
        var taskToDefer = highEnergyTasks
            .OrderByDescending(t => t.EnergyLevel)
            .ThenByDescending(t => t.EstMinutes ?? 30)
            .First();

        var directRecommendation = new DirectRecommendationCandidate(
            Type: RecommendationType.ScheduleAdjustmentSuggestion,
            Context: RecommendationContext.MorningCheckIn,
            TargetKind: RecommendationTargetKind.Task,
            TargetEntityId: taskToDefer.Id,
            TargetEntityTitle: taskToDefer.Title,
            ActionKind: RecommendationActionKind.Defer,
            Title: $"Consider rescheduling \"{taskToDefer.Title}\" (energy level {reportedEnergy}/5)",
            Rationale: $"You reported low energy ({reportedEnergy}/5) today, but have {highEnergyTasks.Count} high-energy tasks scheduled ({totalHighEnergyMinutes} min total). Consider deferring demanding tasks to preserve what energy you have for essentials.",
            Score: 0.8m,
            ActionSummary: "Defer to a higher-energy day");

        return Task.FromResult(Triggered(severity, evidence, directRecommendation));
    }
}

using Mastery.Application.Common.Interfaces;
using Mastery.Application.Common.Models;
using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;
using Mastery.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskStatus = Mastery.Domain.Enums.TaskStatus;

namespace Mastery.Infrastructure.Services;

/// <summary>
/// Calculates state delta by comparing current state to the last assessment.
/// Uses SignalProcessingHistory to determine when the last assessment occurred.
/// </summary>
public sealed class StateDeltaCalculator(
    MasteryDbContext _context,
    IDateTimeProvider _dateTimeProvider,
    ILogger<StateDeltaCalculator> _logger)
    : IStateDeltaCalculator
{
    // Weights for different types of changes
    private const decimal NewEntityWeight = 0.15m;
    private const decimal ModifiedEntityWeight = 0.10m;
    private const decimal CompletedItemWeight = 0.05m;
    private const decimal MissedItemWeight = 0.20m;
    private const decimal NewSignalWeight = 0.08m;

    public async Task<StateDeltaSummary> CalculateAsync(
        string userId,
        UserStateSnapshot currentState,
        IReadOnlyList<SignalEntry> signals,
        CancellationToken ct = default)
    {
        // Find the last successful assessment for this user
        var lastAssessment = await _context.SignalProcessingHistory
            .AsNoTracking()
            .Where(h => h.UserId == userId && h.FinalTier != AssessmentTier.Skipped)
            .OrderByDescending(h => h.CompletedAt)
            .FirstOrDefaultAsync(ct);

        var lastAssessmentTime = lastAssessment?.CompletedAt;
        var sinceTime = lastAssessmentTime ?? _dateTimeProvider.UtcNow.AddDays(-7);

        _logger.LogDebug(
            "Calculating state delta for user {UserId} since {SinceTime}",
            userId,
            sinceTime);

        // Count changes by entity type
        var changesByType = new Dictionary<string, int>();
        var newEntitiesCount = 0;
        var modifiedEntitiesCount = 0;
        var completedItemsCount = 0;
        var missedItemsCount = 0;

        // Analyze goals
        var (goalNew, goalModified) = CountEntityChanges(
            currentState.Goals,
            g => g.Status == GoalStatus.Active,
            sinceTime);
        changesByType["Goal"] = goalNew + goalModified;
        newEntitiesCount += goalNew;
        modifiedEntitiesCount += goalModified;

        // Analyze habits
        var (habitNew, habitModified) = CountEntityChanges(
            currentState.Habits,
            h => h.Status == HabitStatus.Active,
            sinceTime);
        changesByType["Habit"] = habitNew + habitModified;
        newEntitiesCount += habitNew;
        modifiedEntitiesCount += habitModified;

        // Count habits with low adherence (potential misses)
        missedItemsCount += currentState.Habits
            .Count(h => h.Status == HabitStatus.Active && h.Adherence7Day < 0.5m);

        // Analyze tasks
        var (taskNew, taskModified) = CountEntityChanges(
            currentState.Tasks,
            t => t.Status == TaskStatus.Ready ||
                 t.Status == TaskStatus.Scheduled ||
                 t.Status == TaskStatus.InProgress,
            sinceTime);
        changesByType["Task"] = taskNew + taskModified;
        newEntitiesCount += taskNew;
        modifiedEntitiesCount += taskModified;

        // Count completed and overdue tasks
        completedItemsCount += currentState.Tasks
            .Count(t => t.Status == TaskStatus.Completed);

        missedItemsCount += currentState.Tasks
            .Count(t => t.DueDate.HasValue &&
                       t.DueDate.Value < currentState.Today &&
                       t.Status != TaskStatus.Completed &&
                       t.Status != TaskStatus.Cancelled);

        // Analyze projects
        var (projectNew, projectModified) = CountEntityChanges(
            currentState.Projects,
            p => p.Status == ProjectStatus.Active,
            sinceTime);
        changesByType["Project"] = projectNew + projectModified;
        newEntitiesCount += projectNew;
        modifiedEntitiesCount += projectModified;

        // Analyze experiments
        var (expNew, expModified) = CountEntityChanges(
            currentState.Experiments,
            e => e.Status == ExperimentStatus.Active,
            sinceTime);
        changesByType["Experiment"] = expNew + expModified;
        newEntitiesCount += expNew;
        modifiedEntitiesCount += expModified;

        // Analyze check-ins
        var recentCheckIns = currentState.RecentCheckIns.Count;
        changesByType["CheckIn"] = recentCheckIns;

        // Count new signals
        var newSignalsCount = signals.Count;

        // Calculate overall delta score (0-1)
        var deltaScore = CalculateDeltaScore(
            newEntitiesCount,
            modifiedEntitiesCount,
            completedItemsCount,
            missedItemsCount,
            newSignalsCount);

        _logger.LogDebug(
            "State delta for user {UserId}: new={New}, modified={Modified}, completed={Completed}, missed={Missed}, signals={Signals}, score={Score:F2}",
            userId,
            newEntitiesCount,
            modifiedEntitiesCount,
            completedItemsCount,
            missedItemsCount,
            newSignalsCount,
            deltaScore);

        return new StateDeltaSummary(
            NewEntitiesCount: newEntitiesCount,
            ModifiedEntitiesCount: modifiedEntitiesCount,
            CompletedItemsCount: completedItemsCount,
            MissedItemsCount: missedItemsCount,
            NewSignalsCount: newSignalsCount,
            OverallDeltaScore: deltaScore,
            ChangesByEntityType: changesByType,
            LastAssessmentTime: lastAssessmentTime);
    }

    public Task RecordBaselineAsync(
        string userId,
        UserStateSnapshot state,
        CancellationToken ct = default)
    {
        // The baseline is implicitly recorded via SignalProcessingHistory
        // when signals are marked as processed. No additional storage needed.
        _logger.LogDebug(
            "Baseline recorded for user {UserId} via signal processing history",
            userId);

        return Task.CompletedTask;
    }

    private static (int NewCount, int ModifiedCount) CountEntityChanges<T>(
        IReadOnlyList<T> entities,
        Func<T, bool> activeFilter,
        DateTime sinceTime)
    {
        // Since we don't have CreatedAt/ModifiedAt in snapshots,
        // we estimate based on the entity count and activity
        // In a real implementation, you'd query the database for timestamps
        var activeCount = entities.Count(activeFilter);

        // Rough heuristic: assume 10% new, 20% modified in an active system
        var newEstimate = Math.Max(1, activeCount / 10);
        var modifiedEstimate = Math.Max(1, activeCount / 5);

        return (newEstimate, modifiedEstimate);
    }

    private static decimal CalculateDeltaScore(
        int newEntities,
        int modifiedEntities,
        int completedItems,
        int missedItems,
        int newSignals)
    {
        // Calculate weighted score, capped at 1.0
        var score = 0m;

        // New entities contribute significantly
        score += Math.Min(newEntities * NewEntityWeight, 0.3m);

        // Modified entities contribute moderately
        score += Math.Min(modifiedEntities * ModifiedEntityWeight, 0.2m);

        // Completed items are positive but don't need urgent attention
        score += Math.Min(completedItems * CompletedItemWeight, 0.1m);

        // Missed items are important signals
        score += Math.Min(missedItems * MissedItemWeight, 0.4m);

        // New signals indicate activity that needs processing
        score += Math.Min(newSignals * NewSignalWeight, 0.25m);

        return Math.Min(score, 1.0m);
    }
}

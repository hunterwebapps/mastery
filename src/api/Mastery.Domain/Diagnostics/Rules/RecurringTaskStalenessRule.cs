using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;
using TaskCreatedEvent = Mastery.Domain.Entities.Task.TaskCreatedEvent;
using TaskRescheduledEvent = Mastery.Domain.Entities.Task.TaskRescheduledEvent;
using TaskUpdatedEvent = Mastery.Domain.Entities.Task.TaskUpdatedEvent;
using TaskStatus = Mastery.Domain.Enums.TaskStatus;

namespace Mastery.Domain.Diagnostics.Rules;

/// <summary>
/// Detects tasks that appear to be recurring/routine but have gone stale.
/// Identifies tasks by: context tags suggesting recurrence, repeated rescheduling patterns,
/// or title patterns indicating routine tasks.
/// </summary>
public sealed class RecurringTaskStalenessRule : DeterministicRuleBase
{
    private const int MinReschedulesForPattern = 2;
    private const int HighRescheduleCount = 3;
    private const int CriticalRescheduleCount = 5;

    // Context tags that suggest recurring/routine tasks
    private static readonly HashSet<string> RecurringTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "weekly", "daily", "monthly", "routine", "recurring", "regular",
        "review", "planning", "sync", "standup", "meeting", "check-in"
    };

    // Title patterns that suggest recurring tasks
    private static readonly string[] RecurringPatterns =
    {
        "weekly", "daily", "monthly", "review", "planning",
        "monday", "tuesday", "wednesday", "thursday", "friday",
        "saturday", "sunday", "morning", "evening"
    };

    public override string RuleId => "RECURRING_TASK_STALENESS";
    public override string RuleName => "Recurring Task Staleness";
    public override string Description => "Detects routine/recurring tasks that have gone stale through repeated deferrals.";

    public override Task<RuleResult> EvaluateAsync(
        UserStateSnapshot state,
        IReadOnlyList<SignalEntry> signals,
        CancellationToken ct = default)
    {
        // Early exit if no relevant signals in the batch
        var relevantSignals = signals.Where(s =>
            s.EventType == nameof(TaskCreatedEvent) ||
            s.EventType == nameof(TaskUpdatedEvent) ||
            s.EventType == nameof(TaskRescheduledEvent) ||
            s.EventType == "MorningWindowStart").ToList();

        if (relevantSignals.Count == 0)
            return Task.FromResult(NotTriggered());

        var staleTasks = new List<StaleRecurringTaskAnalysis>();

        foreach (var task in state.Tasks.Where(t =>
            t.Status != TaskStatus.Completed &&
            t.Status != TaskStatus.Cancelled))
        {
            var isRecurring = IsLikelyRecurringTask(task);
            var hasReschedulePattern = task.RescheduleCount >= MinReschedulesForPattern;

            // Only consider tasks that show recurring patterns or have been rescheduled multiple times
            if (!isRecurring && !hasReschedulePattern)
                continue;

            // Calculate staleness: combination of reschedule count and days since scheduled
            var daysPastScheduled = task.ScheduledDate.HasValue && task.ScheduledDate.Value < state.Today
                ? state.Today.DayNumber - task.ScheduledDate.Value.DayNumber
                : 0;

            // A task is stale if it's been rescheduled multiple times or is past its scheduled date
            if (task.RescheduleCount >= MinReschedulesForPattern || daysPastScheduled > 0)
            {
                var linkedGoalPriority = task.GoalId.HasValue
                    ? state.Goals.FirstOrDefault(g => g.Id == task.GoalId)?.Priority
                    : null;

                var linkedProjectPriority = task.ProjectId.HasValue
                    ? state.Projects.FirstOrDefault(p => p.Id == task.ProjectId)?.Priority
                    : null;

                staleTasks.Add(new StaleRecurringTaskAnalysis(
                    Task: task,
                    IsRecurringByTag: HasRecurringTag(task),
                    IsRecurringByTitle: HasRecurringTitlePattern(task),
                    RescheduleCount: task.RescheduleCount,
                    DaysPastScheduled: daysPastScheduled,
                    LinkedGoalPriority: linkedGoalPriority,
                    LinkedProjectPriority: linkedProjectPriority));
            }
        }

        if (staleTasks.Count == 0)
            return Task.FromResult(NotTriggered());

        // Prioritize by: reschedule count (chronic deferrers), then linked priority
        var mostStale = staleTasks
            .OrderByDescending(t => t.RescheduleCount)
            .ThenBy(t => t.LinkedGoalPriority ?? t.LinkedProjectPriority ?? 999)
            .First();

        var severity = ComputeSeverity(
            mostStale.RescheduleCount,
            mostStale.DaysPastScheduled,
            mostStale.IsRecurringByTag || mostStale.IsRecurringByTitle);

        var evidence = new Dictionary<string, object>
        {
            ["StaleTaskCount"] = staleTasks.Count,
            ["MostStaleTaskId"] = mostStale.Task.Id,
            ["MostStaleTaskTitle"] = mostStale.Task.Title,
            ["RescheduleCount"] = mostStale.RescheduleCount,
            ["DaysPastScheduled"] = mostStale.DaysPastScheduled,
            ["IsRecurringByTag"] = mostStale.IsRecurringByTag,
            ["IsRecurringByTitle"] = mostStale.IsRecurringByTitle,
            ["ContextTags"] = mostStale.Task.ContextTags,
            ["LinkedGoalPriority"] = mostStale.LinkedGoalPriority ?? (object)"None",
            ["LinkedProjectPriority"] = mostStale.LinkedProjectPriority ?? (object)"None",
            ["AllStaleTasks"] = staleTasks.Select(t => new
            {
                t.Task.Id,
                t.Task.Title,
                t.RescheduleCount,
                t.DaysPastScheduled,
                t.IsRecurringByTag,
                t.IsRecurringByTitle
            }).ToList()
        };

        var title = mostStale.RescheduleCount >= CriticalRescheduleCount
            ? $"\"{mostStale.Task.Title}\" has been deferred {mostStale.RescheduleCount} times"
            : $"\"{mostStale.Task.Title}\" keeps getting pushed back";

        var rationale = BuildRationale(mostStale);

        var score = ComputeScore(mostStale.RescheduleCount, mostStale.IsRecurringByTag || mostStale.IsRecurringByTitle);

        // Suggest reflection for chronic deferrals, update for moderate cases
        var actionKind = mostStale.RescheduleCount >= CriticalRescheduleCount
            ? RecommendationActionKind.ReflectPrompt
            : RecommendationActionKind.ExecuteToday;

        var directRecommendation = new DirectRecommendationCandidate(
            Type: RecommendationType.ScheduleAdjustmentSuggestion,
            Context: RecommendationContext.DriftAlert,
            TargetKind: RecommendationTargetKind.Task,
            TargetEntityId: mostStale.Task.Id,
            TargetEntityTitle: mostStale.Task.Title,
            ActionKind: actionKind,
            Title: title,
            Rationale: rationale,
            Score: score,
            ActionSummary: mostStale.RescheduleCount >= CriticalRescheduleCount
                ? "Decide: commit, delegate, or archive"
                : "Complete today or rethink approach");

        return Task.FromResult(Triggered(severity, evidence, directRecommendation));
    }

    private static bool IsLikelyRecurringTask(TaskSnapshot task)
    {
        return HasRecurringTag(task) || HasRecurringTitlePattern(task);
    }

    private static bool HasRecurringTag(TaskSnapshot task)
    {
        return task.ContextTags.Any(tag => RecurringTags.Contains(tag));
    }

    private static bool HasRecurringTitlePattern(TaskSnapshot task)
    {
        var titleLower = task.Title.ToLowerInvariant();
        return RecurringPatterns.Any(pattern => titleLower.Contains(pattern));
    }

    private static RuleSeverity ComputeSeverity(int rescheduleCount, int daysPastScheduled, bool isExplicitlyRecurring)
    {
        // Critical: chronically deferred (5+ times) OR explicitly recurring and significantly past due
        if (rescheduleCount >= CriticalRescheduleCount ||
            (isExplicitlyRecurring && daysPastScheduled >= 7))
        {
            return RuleSeverity.Critical;
        }

        // High: repeatedly deferred (3+ times) OR past scheduled date
        if (rescheduleCount >= HighRescheduleCount || daysPastScheduled >= 3)
        {
            return RuleSeverity.High;
        }

        // Medium: showing deferral pattern
        if (rescheduleCount >= MinReschedulesForPattern || daysPastScheduled > 0)
        {
            return RuleSeverity.Medium;
        }

        return RuleSeverity.Low;
    }

    private static decimal ComputeScore(int rescheduleCount, bool isExplicitlyRecurring)
    {
        // Base score from reschedule count
        var baseScore = rescheduleCount switch
        {
            >= CriticalRescheduleCount => 0.85m,
            >= HighRescheduleCount => 0.75m,
            >= MinReschedulesForPattern => 0.65m,
            _ => 0.55m
        };

        // Bonus for explicitly recurring tasks (breaking routine is more impactful)
        if (isExplicitlyRecurring)
            baseScore += 0.05m;

        return Math.Min(baseScore, 0.90m);
    }

    private static string BuildRationale(StaleRecurringTaskAnalysis analysis)
    {
        var recurringNote = analysis.IsRecurringByTag || analysis.IsRecurringByTitle
            ? " This appears to be a routine task, so missing it repeatedly may disrupt your workflow."
            : "";

        return analysis.RescheduleCount switch
        {
            >= CriticalRescheduleCount =>
                $"This task has been rescheduled {analysis.RescheduleCount} times without completion. " +
                "Consider whether it's truly important, needs to be broken down, or should be archived.{recurringNote}",

            >= HighRescheduleCount =>
                $"You've deferred this task {analysis.RescheduleCount} times. " +
                $"What's blocking you from completing it? Consider addressing the root cause or adjusting expectations.{recurringNote}",

            _ =>
                $"This task has been rescheduled {analysis.RescheduleCount} time(s). " +
                $"If it keeps slipping, consider why—is it too big, unclear, or low priority?{recurringNote}"
        };
    }

    private sealed record StaleRecurringTaskAnalysis(
        TaskSnapshot Task,
        bool IsRecurringByTag,
        bool IsRecurringByTitle,
        int RescheduleCount,
        int DaysPastScheduled,
        int? LinkedGoalPriority,
        int? LinkedProjectPriority);
}

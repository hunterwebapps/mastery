using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Common;
using Mastery.Domain.Entities;
using Mastery.Domain.Entities.CheckIn;
using Mastery.Domain.Entities.Experiment;
using Mastery.Domain.Entities.Goal;
using Mastery.Domain.Entities.Habit;
using Mastery.Domain.Entities.Metrics;
using Mastery.Domain.Entities.Project;
using Mastery.Domain.Entities.UserProfile;
using Mastery.Domain.Enums;
using Mastery.Domain.Events;

namespace Mastery.Infrastructure.Services;

/// <summary>
/// Classifies domain events into signals with appropriate priority and processing windows.
/// </summary>
public sealed class SignalClassifier : ISignalClassifier
{
    /// <inheritdoc />
    public SignalClassification? ClassifyEvent(IDomainEvent domainEvent, string userId, DateTime userLocalTime)
    {
        return domainEvent switch
        {
            // URGENT (P0) - Immediate processing required
            // Note: These are typically derived signals, not direct domain events.
            // Urgent classification happens through derived signal detection.

            // WINDOW-ALIGNED (P1) - Process at user's natural breakpoints
            MorningCheckInSubmittedEvent e => new SignalClassification(
                SignalPriority.WindowAligned,
                ProcessingWindowType.MorningWindow,
                nameof(MorningCheckInSubmittedEvent),
                nameof(CheckIn),
                e.CheckInId,
                new Dictionary<string, object>
                {
                    ["EnergyLevel"] = e.EnergyLevel,
                    ["SelectedMode"] = e.SelectedMode.ToString()
                }),

            EveningCheckInSubmittedEvent e => new SignalClassification(
                SignalPriority.WindowAligned,
                ProcessingWindowType.EveningWindow,
                nameof(EveningCheckInSubmittedEvent),
                nameof(CheckIn),
                e.CheckInId,
                new Dictionary<string, object>
                {
                    ["Top1Completed"] = e.Top1Completed ?? false
                }),

            CheckInSkippedEvent e => new SignalClassification(
                SignalPriority.WindowAligned,
                e.Type == CheckInType.Morning ? ProcessingWindowType.MorningWindow : ProcessingWindowType.EveningWindow,
                nameof(CheckInSkippedEvent),
                nameof(CheckIn),
                e.CheckInId,
                new Dictionary<string, object>
                {
                    ["CheckInType"] = e.Type.ToString()
                }),

            // STANDARD (P2) - Batch processing within 4-6 hours
            HabitCompletedEvent e => new SignalClassification(
                SignalPriority.Standard,
                ProcessingWindowType.BatchWindow,
                nameof(HabitCompletedEvent),
                nameof(Habit),
                e.HabitId,
                new Dictionary<string, object>
                {
                    ["OccurrenceId"] = e.OccurrenceId,
                    ["CompletedOn"] = e.CompletedOn.ToString("O"),
                    ["ModeUsed"] = e.ModeUsed?.ToString() ?? "null"
                }),

            HabitMissedEvent e => new SignalClassification(
                SignalPriority.Standard,
                ProcessingWindowType.BatchWindow,
                nameof(HabitMissedEvent),
                nameof(Habit),
                e.HabitId,
                new Dictionary<string, object>
                {
                    ["OccurrenceId"] = e.OccurrenceId,
                    ["ScheduledOn"] = e.ScheduledOn.ToString("O"),
                    ["Reason"] = e.Reason.ToString()
                }),

            HabitSkippedEvent e => new SignalClassification(
                SignalPriority.Standard,
                ProcessingWindowType.BatchWindow,
                nameof(HabitSkippedEvent),
                nameof(Habit),
                e.HabitId,
                new Dictionary<string, object>
                {
                    ["OccurrenceId"] = e.OccurrenceId,
                    ["ScheduledOn"] = e.ScheduledOn.ToString("O")
                }),

            TaskCompletedEvent e => new SignalClassification(
                SignalPriority.Standard,
                ProcessingWindowType.BatchWindow,
                nameof(TaskCompletedEvent),
                nameof(Task),
                e.TaskId),

            TaskRescheduledEvent e => new SignalClassification(
                SignalPriority.Standard,
                ProcessingWindowType.BatchWindow,
                nameof(TaskRescheduledEvent),
                nameof(Task),
                e.TaskId,
                new Dictionary<string, object>
                {
                    ["NewDate"] = e.NewDate.ToString("O"),
                    ["Reason"] = e.Reason?.ToString() ?? "None"
                }),

            GoalStatusChangedEvent e => new SignalClassification(
                SignalPriority.Standard,
                ProcessingWindowType.BatchWindow,
                nameof(GoalStatusChangedEvent),
                nameof(Goal),
                e.GoalId,
                new Dictionary<string, object>
                {
                    ["NewStatus"] = e.NewStatus.ToString()
                }),

            MetricObservationRecordedEvent e => new SignalClassification(
                SignalPriority.Standard,
                ProcessingWindowType.BatchWindow,
                nameof(MetricObservationRecordedEvent),
                nameof(MetricObservation),
                e.ObservationId,
                new Dictionary<string, object>
                {
                    ["MetricDefinitionId"] = e.MetricDefinitionId,
                    ["Value"] = e.Value
                }),

            ExperimentStartedEvent e => new SignalClassification(
                SignalPriority.Standard,
                ProcessingWindowType.BatchWindow,
                nameof(ExperimentStartedEvent),
                nameof(Experiment),
                e.ExperimentId),

            ExperimentCompletedEvent e => new SignalClassification(
                SignalPriority.Standard,
                ProcessingWindowType.BatchWindow,
                nameof(ExperimentCompletedEvent),
                nameof(Experiment),
                e.ExperimentId,
                new Dictionary<string, object>
                {
                    ["Outcome"] = e.Outcome.ToString()
                }),

            ProjectStatusChangedEvent e => new SignalClassification(
                SignalPriority.Standard,
                ProcessingWindowType.BatchWindow,
                nameof(ProjectStatusChangedEvent),
                nameof(Project),
                e.ProjectId,
                new Dictionary<string, object>
                {
                    ["NewStatus"] = e.NewStatus.ToString()
                }),

            HabitStreakMilestoneEvent e => new SignalClassification(
                SignalPriority.Standard,
                ProcessingWindowType.BatchWindow,
                nameof(HabitStreakMilestoneEvent),
                nameof(Habit),
                e.HabitId,
                new Dictionary<string, object>
                {
                    ["StreakCount"] = e.StreakCount,
                    ["MilestoneType"] = e.MilestoneType
                }),

            // LOW (P3) - Background processing within 24 hours
            HabitCreatedEvent e => new SignalClassification(
                SignalPriority.Low,
                ProcessingWindowType.BatchWindow,
                nameof(HabitCreatedEvent),
                nameof(Habit),
                e.HabitId),

            HabitUpdatedEvent e => new SignalClassification(
                SignalPriority.Low,
                ProcessingWindowType.BatchWindow,
                nameof(HabitUpdatedEvent),
                nameof(Habit),
                e.HabitId,
                new Dictionary<string, object>
                {
                    ["ChangedSection"] = e.ChangedSection
                }),

            HabitStatusChangedEvent e => new SignalClassification(
                SignalPriority.Low,
                ProcessingWindowType.BatchWindow,
                nameof(HabitStatusChangedEvent),
                nameof(Habit),
                e.HabitId,
                new Dictionary<string, object>
                {
                    ["NewStatus"] = e.NewStatus.ToString()
                }),

            HabitArchivedEvent e => new SignalClassification(
                SignalPriority.Low,
                ProcessingWindowType.BatchWindow,
                nameof(HabitArchivedEvent),
                nameof(Habit),
                e.HabitId),

            GoalCreatedEvent e => new SignalClassification(
                SignalPriority.Low,
                ProcessingWindowType.BatchWindow,
                nameof(GoalCreatedEvent),
                nameof(Goal),
                e.GoalId),

            GoalUpdatedEvent e => new SignalClassification(
                SignalPriority.Low,
                ProcessingWindowType.BatchWindow,
                nameof(GoalUpdatedEvent),
                nameof(Goal),
                e.GoalId),

            TaskCreatedEvent e => new SignalClassification(
                SignalPriority.Low,
                ProcessingWindowType.BatchWindow,
                nameof(TaskCreatedEvent),
                nameof(Task),
                e.TaskId),

            TaskUpdatedEvent e => new SignalClassification(
                SignalPriority.Low,
                ProcessingWindowType.BatchWindow,
                nameof(TaskUpdatedEvent),
                nameof(Task),
                e.TaskId),

            TaskArchivedEvent e => new SignalClassification(
                SignalPriority.Low,
                ProcessingWindowType.BatchWindow,
                nameof(TaskArchivedEvent),
                nameof(Task),
                e.TaskId),

            ProjectCreatedEvent e => new SignalClassification(
                SignalPriority.Low,
                ProcessingWindowType.BatchWindow,
                nameof(ProjectCreatedEvent),
                nameof(Project),
                e.ProjectId),

            ProjectUpdatedEvent e => new SignalClassification(
                SignalPriority.Low,
                ProcessingWindowType.BatchWindow,
                nameof(ProjectUpdatedEvent),
                nameof(Project),
                e.ProjectId),

            ExperimentCreatedEvent e => new SignalClassification(
                SignalPriority.Low,
                ProcessingWindowType.BatchWindow,
                nameof(ExperimentCreatedEvent),
                nameof(Experiment),
                e.ExperimentId),

            UserProfileUpdatedEvent => new SignalClassification(
                SignalPriority.Low,
                ProcessingWindowType.BatchWindow,
                nameof(UserProfileUpdatedEvent),
                nameof(UserProfile),
                null),

            SeasonCreatedEvent e => new SignalClassification(
                SignalPriority.Low,
                ProcessingWindowType.BatchWindow,
                nameof(SeasonCreatedEvent),
                nameof(Season),
                e.SeasonId),

            CheckInUpdatedEvent e => new SignalClassification(
                SignalPriority.Low,
                ProcessingWindowType.BatchWindow,
                nameof(CheckInUpdatedEvent),
                nameof(CheckIn),
                e.CheckInId),

            // Events that don't generate signals (handled elsewhere or internal)
            HabitUndoneEvent => null,
            HabitModeSuggestedEvent => null,
            RecommendationAcceptedEvent => null,
            RecommendationDismissedEvent => null,
            RecommendationSnoozedEvent => null,
            DiagnosticSignalDetectedEvent => null, // Handled by diagnostic system
            TaskStatusChangedEvent => null, // Covered by specific events like TaskCompletedEvent
            TaskCompletionUndoneEvent => null,
            TaskCancelledEvent => null,
            TaskScheduledEvent => null,
            TaskDependencyAddedEvent => null,
            TaskDependencyRemovedEvent => null,
            GoalCompletedEvent => null, // Covered by GoalStatusChangedEvent
            GoalScoreboardUpdatedEvent => null, // Internal scoreboard updates
            MetricObservationCorrectedEvent => null,
            ExperimentPausedEvent => null,
            ExperimentResumedEvent => null,
            ExperimentAbandonedEvent => null,
            ProjectNextActionSetEvent => null,
            ProjectCompletedEvent => null, // Covered by ProjectStatusChangedEvent
            MilestoneAddedEvent => null,
            MilestoneCompletedEvent => null,
            SeasonActivatedEvent => null,
            SeasonEndedEvent => null,
            UserProfileCreatedEvent => null,
            PreferencesUpdatedEvent => null,
            ConstraintsUpdatedEvent => null,
            MetricDefinitionCreatedEvent => null,
            MetricDefinitionUpdatedEvent => null,
            MetricDefinitionArchivedEvent => null,
            RecommendationsGeneratedEvent => null,

            // Unknown events - log and skip
            _ => null
        };
    }

    /// <inheritdoc />
    public bool ShouldEscalateToUrgent(IReadOnlyList<SignalClassification> pendingSignals, object? state)
    {
        // Escalation logic based on signal patterns:
        // 1. Multiple missed habits in a row (adherence drop)
        // 2. Multiple task reschedules (capacity overload signal)
        // 3. Check-in skips combined with misses (disengagement signal)

        var missedHabits = pendingSignals.Count(s => s.EventType == nameof(HabitMissedEvent));
        var rescheduledTasks = pendingSignals.Count(s => s.EventType == nameof(TaskRescheduledEvent));
        var skippedCheckIns = pendingSignals.Count(s => s.EventType == nameof(CheckInSkippedEvent));

        // Escalate if clear pattern of overload or disengagement
        if (missedHabits >= 3)
            return true;

        if (rescheduledTasks >= 3)
            return true;

        if (skippedCheckIns >= 2 && missedHabits >= 1)
            return true;

        return false;
    }
}

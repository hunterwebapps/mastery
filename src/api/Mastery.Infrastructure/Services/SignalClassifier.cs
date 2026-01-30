using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Enums;
using Mastery.Domain.Events;

namespace Mastery.Infrastructure.Services;

/// <summary>
/// Classifies domain events into signals with appropriate priority and processing windows.
/// </summary>
public sealed class SignalClassifier : ISignalClassifier
{
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

    /// <inheritdoc />
    public SignalClassification? ClassifyOutboxEntry(
        string entityType,
        Guid entityId,
        string? domainEventType,
        string? userId)
    {
        // Skip if no user ID or no event type
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(domainEventType))
            return null;

        // Map domain event type name to priority and window
        // Note: P0 (Urgent) signals are detected at processing time by Tier 0 rules,
        // not at classification time from outbox entries
        return domainEventType switch
        {
            // P1 (Window-Aligned) - Check-ins
            nameof(MorningCheckInSubmittedEvent) => new SignalClassification(
                SignalPriority.WindowAligned, ProcessingWindowType.MorningWindow,
                domainEventType, entityType, entityId),
            nameof(EveningCheckInSubmittedEvent) => new SignalClassification(
                SignalPriority.WindowAligned, ProcessingWindowType.EveningWindow,
                domainEventType, entityType, entityId),
            nameof(CheckInSkippedEvent) => new SignalClassification(
                SignalPriority.WindowAligned, ProcessingWindowType.BatchWindow,
                domainEventType, entityType, entityId),

            // P2 (Standard) - Behavioral signals
            nameof(HabitCompletedEvent) or nameof(HabitMissedEvent) or nameof(HabitSkippedEvent) =>
                new SignalClassification(SignalPriority.Standard, ProcessingWindowType.BatchWindow,
                    domainEventType, entityType, entityId),
            nameof(TaskCompletedEvent) or nameof(TaskRescheduledEvent) =>
                new SignalClassification(SignalPriority.Standard, ProcessingWindowType.BatchWindow,
                    domainEventType, entityType, entityId),
            nameof(MetricObservationRecordedEvent) =>
                new SignalClassification(SignalPriority.Standard, ProcessingWindowType.BatchWindow,
                    domainEventType, entityType, entityId),
            nameof(ExperimentStartedEvent) or nameof(ExperimentCompletedEvent) =>
                new SignalClassification(SignalPriority.Standard, ProcessingWindowType.BatchWindow,
                    domainEventType, entityType, entityId),
            nameof(GoalStatusChangedEvent) or nameof(ProjectStatusChangedEvent) =>
                new SignalClassification(SignalPriority.Standard, ProcessingWindowType.BatchWindow,
                    domainEventType, entityType, entityId),
            nameof(HabitStreakMilestoneEvent) =>
                new SignalClassification(SignalPriority.Standard, ProcessingWindowType.BatchWindow,
                    domainEventType, entityType, entityId),

            // P3 (Low) - Metadata changes
            nameof(GoalCreatedEvent) or nameof(GoalUpdatedEvent) or
            nameof(HabitCreatedEvent) or nameof(HabitUpdatedEvent) or nameof(HabitStatusChangedEvent) or nameof(HabitArchivedEvent) or
            nameof(TaskCreatedEvent) or nameof(TaskUpdatedEvent) or nameof(TaskArchivedEvent) or
            nameof(ProjectCreatedEvent) or nameof(ProjectUpdatedEvent) or
            nameof(ExperimentCreatedEvent) or
            nameof(UserProfileUpdatedEvent) or nameof(SeasonCreatedEvent) or nameof(CheckInUpdatedEvent) =>
                new SignalClassification(SignalPriority.Low, ProcessingWindowType.BatchWindow,
                    domainEventType, entityType, entityId),

            // Skip events that don't need signals
            _ => null
        };
    }
}

using Mastery.Domain.Common;
using Mastery.Domain.Enums;

namespace Mastery.Application.Common.Interfaces;

/// <summary>
/// Classifies domain events into signals with priority and processing window information.
/// </summary>
public interface ISignalClassifier
{
    /// <summary>
    /// Classifies a domain event into a signal with priority and processing window.
    /// </summary>
    /// <param name="domainEvent">The domain event to classify.</param>
    /// <param name="userId">The user ID associated with the event.</param>
    /// <param name="userLocalTime">The current time in the user's local timezone.</param>
    /// <returns>The signal classification, or null if the event should not generate a signal.</returns>
    SignalClassification? ClassifyEvent(IDomainEvent domainEvent, string userId, DateTime userLocalTime);

    /// <summary>
    /// Determines if a collection of accumulated signals should be escalated to urgent priority.
    /// </summary>
    /// <param name="pendingSignals">The signals currently pending for the user.</param>
    /// <param name="state">The current user state snapshot.</param>
    /// <returns>True if the signals should be escalated to urgent processing.</returns>
    bool ShouldEscalateToUrgent(IReadOnlyList<SignalClassification> pendingSignals, object? state);
}

/// <summary>
/// Represents the classification result for a domain event.
/// </summary>
public sealed record SignalClassification(
    /// <summary>
    /// The priority level for processing this signal.
    /// </summary>
    SignalPriority Priority,

    /// <summary>
    /// The type of processing window for this signal.
    /// </summary>
    ProcessingWindowType WindowType,

    /// <summary>
    /// The type of the domain event that generated this signal.
    /// </summary>
    string EventType,

    /// <summary>
    /// The type of target entity (e.g., "Goal", "Habit", "CheckIn").
    /// </summary>
    string? TargetEntityType,

    /// <summary>
    /// The ID of the target entity.
    /// </summary>
    Guid? TargetEntityId,

    /// <summary>
    /// Additional metadata about the signal.
    /// </summary>
    IReadOnlyDictionary<string, object>? Metadata = null
);

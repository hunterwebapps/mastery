using Mastery.Domain.Enums;

namespace Mastery.Domain.Common;

/// <summary>
/// Marks a domain event with its signal classification metadata.
/// Used by SignalClassifier to determine how to process the event.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class SignalClassificationAttribute : Attribute
{
    /// <summary>
    /// The priority level for processing this signal.
    /// </summary>
    public SignalPriority Priority { get; }

    /// <summary>
    /// The type of processing window for this signal.
    /// </summary>
    public ProcessingWindowType WindowType { get; }

    /// <summary>
    /// Optional rationale explaining why this classification was chosen.
    /// Useful for documentation and debugging.
    /// </summary>
    public string? Rationale { get; init; }

    public SignalClassificationAttribute(SignalPriority priority, ProcessingWindowType windowType)
    {
        Priority = priority;
        WindowType = windowType;
    }
}

/// <summary>
/// Marks a domain event as not requiring signal processing.
/// Use this for internal lifecycle events, corrections, or events that don't affect recommendations.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class NoSignalAttribute : Attribute
{
    /// <summary>
    /// Optional reason explaining why this event doesn't need signal processing.
    /// </summary>
    public string? Reason { get; init; }
}

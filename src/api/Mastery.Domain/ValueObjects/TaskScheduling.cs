using System.Text.Json.Serialization;
using Mastery.Domain.Common;
using Mastery.Domain.Exceptions;

namespace Mastery.Domain.ValueObjects;

/// <summary>
/// Represents the scheduling configuration for a task.
/// Used when a task is moved to Scheduled status.
/// </summary>
public sealed class TaskScheduling : ValueObject
{
    /// <summary>
    /// The date the task is scheduled for.
    /// </summary>
    public DateOnly ScheduledOn { get; }

    /// <summary>
    /// Optional preferred time window for execution.
    /// </summary>
    public TimeWindow? PreferredTimeWindow { get; }

    // Required for EF Core and JSON deserialization
    private TaskScheduling()
    {
        ScheduledOn = default;
        PreferredTimeWindow = null;
    }

    [JsonConstructor]
    public TaskScheduling(DateOnly scheduledOn, TimeWindow? preferredTimeWindow)
    {
        ScheduledOn = scheduledOn;
        PreferredTimeWindow = preferredTimeWindow;
    }

    /// <summary>
    /// Creates a new TaskScheduling with the specified parameters.
    /// </summary>
    public static TaskScheduling Create(DateOnly scheduledOn, TimeWindow? preferredTimeWindow = null)
    {
        if (scheduledOn == default)
            throw new DomainException("Scheduled date must be specified.");

        return new TaskScheduling(scheduledOn, preferredTimeWindow);
    }

    /// <summary>
    /// Creates scheduling for today.
    /// </summary>
    public static TaskScheduling Today(TimeWindow? preferredTimeWindow = null)
    {
        return new TaskScheduling(DateOnly.FromDateTime(DateTime.UtcNow), preferredTimeWindow);
    }

    /// <summary>
    /// Creates scheduling for tomorrow.
    /// </summary>
    public static TaskScheduling Tomorrow(TimeWindow? preferredTimeWindow = null)
    {
        return new TaskScheduling(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), preferredTimeWindow);
    }

    /// <summary>
    /// Checks if the task is scheduled for the given date.
    /// </summary>
    public bool IsScheduledFor(DateOnly date)
    {
        return ScheduledOn == date;
    }

    /// <summary>
    /// Returns a new TaskScheduling with updated date.
    /// </summary>
    public TaskScheduling WithDate(DateOnly newDate)
    {
        return new TaskScheduling(newDate, PreferredTimeWindow);
    }

    /// <summary>
    /// Returns a new TaskScheduling with updated time window.
    /// </summary>
    public TaskScheduling WithTimeWindow(TimeWindow? timeWindow)
    {
        return new TaskScheduling(ScheduledOn, timeWindow);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ScheduledOn;
        yield return PreferredTimeWindow;
    }

    public override string ToString()
    {
        var windowStr = PreferredTimeWindow != null ? $" ({PreferredTimeWindow})" : "";
        return $"{ScheduledOn:yyyy-MM-dd}{windowStr}";
    }
}

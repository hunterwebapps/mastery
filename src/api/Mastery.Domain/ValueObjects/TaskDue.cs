using System.Text.Json.Serialization;
using Mastery.Domain.Common;
using Mastery.Domain.Enums;

namespace Mastery.Domain.ValueObjects;

/// <summary>
/// Represents the due date configuration for a task.
/// Distinguishes between soft and hard due dates for psychological safety.
/// </summary>
public sealed class TaskDue : ValueObject
{
    /// <summary>
    /// The date the task is due.
    /// </summary>
    public DateOnly? DueOn { get; }

    /// <summary>
    /// Optional specific time the task is due.
    /// </summary>
    public TimeOnly? DueAt { get; }

    /// <summary>
    /// Whether this is a soft (guidance) or hard (commitment) due date.
    /// </summary>
    public DueType DueType { get; }

    // Required for EF Core and JSON deserialization
    private TaskDue()
    {
        DueOn = null;
        DueAt = null;
        DueType = DueType.Soft;
    }

    [JsonConstructor]
    public TaskDue(DateOnly? dueOn, TimeOnly? dueAt, DueType dueType)
    {
        DueOn = dueOn;
        DueAt = dueAt;
        DueType = dueType;
    }

    /// <summary>
    /// Creates a new TaskDue with the specified parameters.
    /// </summary>
    public static TaskDue Create(DateOnly dueOn, TimeOnly? dueAt = null, DueType dueType = DueType.Soft)
    {
        return new TaskDue(dueOn, dueAt, dueType);
    }

    /// <summary>
    /// Creates a soft due date.
    /// </summary>
    public static TaskDue SoftDue(DateOnly dueOn, TimeOnly? dueAt = null)
    {
        return new TaskDue(dueOn, dueAt, DueType.Soft);
    }

    /// <summary>
    /// Creates a hard due date.
    /// </summary>
    public static TaskDue HardDue(DateOnly dueOn, TimeOnly? dueAt = null)
    {
        return new TaskDue(dueOn, dueAt, DueType.Hard);
    }

    /// <summary>
    /// Checks if the task is overdue as of the given date.
    /// Only hard due dates are considered overdue.
    /// </summary>
    public bool IsOverdue(DateOnly today)
    {
        if (DueType != DueType.Hard || DueOn == null)
            return false;

        return DueOn < today;
    }

    /// <summary>
    /// Checks if the task is due on the given date.
    /// </summary>
    public bool IsDueOn(DateOnly date)
    {
        return DueOn == date;
    }

    /// <summary>
    /// Returns a new TaskDue with updated due type.
    /// </summary>
    public TaskDue WithDueType(DueType dueType)
    {
        return new TaskDue(DueOn, DueAt, dueType);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DueOn;
        yield return DueAt;
        yield return DueType;
    }

    public override string ToString()
    {
        if (DueOn == null) return "No due date";
        var typeLabel = DueType == DueType.Hard ? "Hard" : "Soft";
        var timeStr = DueAt.HasValue ? $" at {DueAt:HH:mm}" : "";
        return $"{typeLabel}: {DueOn:yyyy-MM-dd}{timeStr}";
    }
}

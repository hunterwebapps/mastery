using System.Text.Json.Serialization;
using Mastery.Domain.Common;
using Mastery.Domain.Exceptions;

namespace Mastery.Domain.ValueObjects;

/// <summary>
/// Represents the completion data for a task.
/// Captures when, how long, and any notes about the completion.
/// </summary>
public sealed class TaskCompletion : ValueObject
{
    /// <summary>
    /// UTC timestamp when the task was completed.
    /// </summary>
    public DateTime CompletedAtUtc { get; }

    /// <summary>
    /// The date the task was completed in the user's local timezone.
    /// Used for reporting and aggregation.
    /// </summary>
    public DateOnly CompletedOn { get; }

    /// <summary>
    /// Actual minutes spent on the task (optional).
    /// Can be used for time tracking and metric contributions.
    /// </summary>
    public int? ActualMinutes { get; }

    /// <summary>
    /// Optional note about the completion.
    /// </summary>
    public string? CompletionNote { get; }

    /// <summary>
    /// Value entered by user at completion (for UseEnteredValue contribution type).
    /// </summary>
    public decimal? EnteredValue { get; }

    // Required for EF Core and JSON deserialization
    private TaskCompletion()
    {
        CompletedAtUtc = default;
        CompletedOn = default;
        ActualMinutes = null;
        CompletionNote = null;
        EnteredValue = null;
    }

    [JsonConstructor]
    public TaskCompletion(
        DateTime completedAtUtc,
        DateOnly completedOn,
        int? actualMinutes,
        string? completionNote,
        decimal? enteredValue)
    {
        CompletedAtUtc = completedAtUtc;
        CompletedOn = completedOn;
        ActualMinutes = actualMinutes;
        CompletionNote = completionNote;
        EnteredValue = enteredValue;
    }

    /// <summary>
    /// Creates a new TaskCompletion with the specified parameters.
    /// </summary>
    public static TaskCompletion Create(
        DateTime completedAtUtc,
        DateOnly completedOn,
        int? actualMinutes = null,
        string? completionNote = null,
        decimal? enteredValue = null)
    {
        if (completedAtUtc == default)
            throw new DomainException("Completed timestamp must be specified.");

        if (completedOn == default)
            throw new DomainException("Completed date must be specified.");

        if (actualMinutes.HasValue && actualMinutes.Value < 0)
            throw new DomainException("Actual minutes cannot be negative.");

        return new TaskCompletion(completedAtUtc, completedOn, actualMinutes, completionNote, enteredValue);
    }

    /// <summary>
    /// Creates a completion for right now.
    /// </summary>
    public static TaskCompletion Now(
        DateOnly completedOn,
        int? actualMinutes = null,
        string? completionNote = null,
        decimal? enteredValue = null)
    {
        return Create(DateTime.UtcNow, completedOn, actualMinutes, completionNote, enteredValue);
    }

    /// <summary>
    /// Returns a new TaskCompletion with updated actual minutes.
    /// </summary>
    public TaskCompletion WithActualMinutes(int actualMinutes)
    {
        if (actualMinutes < 0)
            throw new DomainException("Actual minutes cannot be negative.");

        return new TaskCompletion(CompletedAtUtc, CompletedOn, actualMinutes, CompletionNote, EnteredValue);
    }

    /// <summary>
    /// Returns a new TaskCompletion with updated note.
    /// </summary>
    public TaskCompletion WithNote(string? note)
    {
        return new TaskCompletion(CompletedAtUtc, CompletedOn, ActualMinutes, note, EnteredValue);
    }

    /// <summary>
    /// Returns a new TaskCompletion with updated entered value.
    /// </summary>
    public TaskCompletion WithEnteredValue(decimal? value)
    {
        return new TaskCompletion(CompletedAtUtc, CompletedOn, ActualMinutes, CompletionNote, value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CompletedAtUtc;
        yield return CompletedOn;
        yield return ActualMinutes;
        yield return CompletionNote;
        yield return EnteredValue;
    }

    public override string ToString()
    {
        var minutesStr = ActualMinutes.HasValue ? $" ({ActualMinutes}min)" : "";
        return $"Completed on {CompletedOn:yyyy-MM-dd}{minutesStr}";
    }
}

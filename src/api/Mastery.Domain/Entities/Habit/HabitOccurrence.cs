using Mastery.Domain.Common;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;

namespace Mastery.Domain.Entities.Habit;

/// <summary>
/// Represents a single occurrence (scheduled instance) of a habit.
/// Generated lazily when the user interacts with a habit on a specific date.
/// </summary>
public sealed class HabitOccurrence : BaseEntity
{
    /// <summary>
    /// The habit this occurrence belongs to.
    /// </summary>
    public Guid HabitId { get; private set; }

    /// <summary>
    /// The date this occurrence is scheduled for.
    /// </summary>
    public DateOnly ScheduledOn { get; private set; }

    /// <summary>
    /// The current status of this occurrence.
    /// </summary>
    public HabitOccurrenceStatus Status { get; private set; }

    /// <summary>
    /// When the occurrence was completed (if applicable).
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// The date the user considers this completed on (in their timezone).
    /// May differ from ScheduledOn for late completions.
    /// </summary>
    public DateOnly? CompletedOn { get; private set; }

    /// <summary>
    /// The mode used when completing this occurrence.
    /// </summary>
    public HabitMode? ModeUsed { get; private set; }

    /// <summary>
    /// User-entered value (for UseEnteredValue contribution type).
    /// </summary>
    public decimal? EnteredValue { get; private set; }

    /// <summary>
    /// Reason for missing (when status is Missed).
    /// </summary>
    public MissReason? MissReason { get; private set; }

    /// <summary>
    /// Optional note for this occurrence.
    /// </summary>
    public string? Note { get; private set; }

    /// <summary>
    /// If rescheduled, the new date this occurrence was moved to.
    /// </summary>
    public DateOnly? RescheduledTo { get; private set; }

    /// <summary>
    /// When this occurrence record was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    private HabitOccurrence() { } // EF Core

    public static HabitOccurrence Create(Guid habitId, DateOnly scheduledOn)
    {
        if (habitId == Guid.Empty)
            throw new DomainException("HabitId cannot be empty.");

        return new HabitOccurrence
        {
            HabitId = habitId,
            ScheduledOn = scheduledOn,
            Status = HabitOccurrenceStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Complete(decimal? enteredValue = null, HabitMode? mode = null, string? note = null)
    {
        if (Status == HabitOccurrenceStatus.Completed)
            throw new DomainException("Occurrence is already completed.");

        if (Status == HabitOccurrenceStatus.Rescheduled)
            throw new DomainException("Cannot complete a rescheduled occurrence.");

        Status = HabitOccurrenceStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        CompletedOn = ScheduledOn; // Could be customized for late completions
        ModeUsed = mode;
        EnteredValue = enteredValue;
        Note = note;
        MissReason = null; // Clear any miss reason
    }

    public void Undo()
    {
        if (Status != HabitOccurrenceStatus.Completed)
            throw new DomainException("Only completed occurrences can be undone.");

        Status = HabitOccurrenceStatus.Pending;
        CompletedAt = null;
        CompletedOn = null;
        ModeUsed = null;
        EnteredValue = null;
        // Note is preserved
    }

    public void Skip(string? reason = null)
    {
        if (Status == HabitOccurrenceStatus.Completed)
            throw new DomainException("Cannot skip a completed occurrence.");

        if (Status == HabitOccurrenceStatus.Rescheduled)
            throw new DomainException("Cannot skip a rescheduled occurrence.");

        Status = HabitOccurrenceStatus.Skipped;
        Note = reason;
    }

    public void MarkMissed(MissReason reason, string? details = null)
    {
        if (Status == HabitOccurrenceStatus.Completed)
            throw new DomainException("Cannot mark a completed occurrence as missed.");

        if (Status == HabitOccurrenceStatus.Rescheduled)
            throw new DomainException("Cannot mark a rescheduled occurrence as missed.");

        Status = HabitOccurrenceStatus.Missed;
        MissReason = reason;
        Note = details;
    }

    public void Reschedule(DateOnly newDate)
    {
        if (Status == HabitOccurrenceStatus.Completed)
            throw new DomainException("Cannot reschedule a completed occurrence.");

        if (newDate <= ScheduledOn)
            throw new DomainException("New date must be after the original scheduled date.");

        Status = HabitOccurrenceStatus.Rescheduled;
        RescheduledTo = newDate;
    }

    public void UpdateNote(string? note)
    {
        Note = note;
    }

    #region Query Helpers

    public bool IsCompleted => Status == HabitOccurrenceStatus.Completed;

    public bool IsPending => Status == HabitOccurrenceStatus.Pending;

    public bool WasMissed => Status == HabitOccurrenceStatus.Missed;

    public bool WasSkipped => Status == HabitOccurrenceStatus.Skipped;

    public bool WasRescheduled => Status == HabitOccurrenceStatus.Rescheduled;

    #endregion
}

using Mastery.Domain.Common;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;

namespace Mastery.Domain.Entities.Project;

/// <summary>
/// Represents a milestone within a project.
/// Milestones are checkpoints for tracking project progress.
/// </summary>
public sealed class Milestone : AuditableEntity
{
    /// <summary>
    /// The project this milestone belongs to.
    /// </summary>
    public Guid ProjectId { get; private set; }

    /// <summary>
    /// The title of the milestone.
    /// </summary>
    public string Title { get; private set; } = null!;

    /// <summary>
    /// Optional target date for the milestone.
    /// </summary>
    public DateOnly? TargetDate { get; private set; }

    /// <summary>
    /// Current status of the milestone.
    /// </summary>
    public MilestoneStatus Status { get; private set; }

    /// <summary>
    /// Optional notes about the milestone.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Display order for sorting milestones.
    /// </summary>
    [EmbeddingIgnore]
    public int DisplayOrder { get; private set; }

    /// <summary>
    /// When the milestone was completed.
    /// </summary>
    public DateTime? CompletedAtUtc { get; private set; }

    private Milestone() { } // EF Core

    public static Milestone Create(
        Guid projectId,
        string title,
        DateOnly? targetDate = null,
        string? notes = null,
        int displayOrder = 0)
    {
        if (projectId == Guid.Empty)
            throw new DomainException("ProjectId cannot be empty.");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Milestone title cannot be empty.");

        if (title.Length > 200)
            throw new DomainException("Milestone title cannot exceed 200 characters.");

        return new Milestone
        {
            ProjectId = projectId,
            Title = title,
            TargetDate = targetDate,
            Status = MilestoneStatus.NotStarted,
            Notes = notes,
            DisplayOrder = displayOrder
        };
    }

    public void Update(
        string? title = null,
        DateOnly? targetDate = null,
        string? notes = null,
        int? displayOrder = null)
    {
        if (Status == MilestoneStatus.Completed)
            throw new DomainException("Cannot update a completed milestone.");

        if (title != null)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Milestone title cannot be empty.");
            if (title.Length > 200)
                throw new DomainException("Milestone title cannot exceed 200 characters.");
            Title = title;
        }

        if (targetDate.HasValue)
            TargetDate = targetDate;

        if (notes != null)
            Notes = notes;

        if (displayOrder.HasValue)
            DisplayOrder = displayOrder.Value;
    }

    /// <summary>
    /// Marks the milestone as in progress.
    /// </summary>
    public void StartProgress()
    {
        if (Status != MilestoneStatus.NotStarted)
            throw new DomainException($"Only NotStarted milestones can be started. Current status: {Status}");

        Status = MilestoneStatus.InProgress;
    }

    /// <summary>
    /// Marks the milestone as completed.
    /// </summary>
    public void Complete()
    {
        if (Status == MilestoneStatus.Completed)
            throw new DomainException("Milestone is already completed.");

        Status = MilestoneStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Undoes the completion and returns to InProgress status.
    /// </summary>
    public void UndoCompletion()
    {
        if (Status != MilestoneStatus.Completed)
            throw new DomainException("Only completed milestones can be undone.");

        Status = MilestoneStatus.InProgress;
        CompletedAtUtc = null;
    }

    /// <summary>
    /// Clears the target date.
    /// </summary>
    public void ClearTargetDate()
    {
        if (Status == MilestoneStatus.Completed)
            throw new DomainException("Cannot modify a completed milestone.");

        TargetDate = null;
    }
}

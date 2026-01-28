using Mastery.Domain.Common;
using Mastery.Domain.Exceptions;

namespace Mastery.Domain.Entities.Experiment;

/// <summary>
/// Represents a note attached to an experiment.
/// Notes capture observations, insights, or context during the experiment run.
/// </summary>
public sealed class ExperimentNote : BaseEntity
{
    /// <summary>
    /// The experiment this note belongs to.
    /// </summary>
    public Guid ExperimentId { get; private set; }

    /// <summary>
    /// The text content of the note.
    /// </summary>
    public string Content { get; private set; } = null!;

    /// <summary>
    /// When the note was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    private ExperimentNote() { } // EF Core

    public static ExperimentNote Create(Guid experimentId, string content)
    {
        if (experimentId == Guid.Empty)
            throw new DomainException("ExperimentId cannot be empty.");

        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("Note content cannot be empty.");

        if (content.Length > 2000)
            throw new DomainException("Note content cannot exceed 2000 characters.");

        return new ExperimentNote
        {
            ExperimentId = experimentId,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };
    }
}

using Mastery.Domain.Common;
using Mastery.Domain.Enums;
using Mastery.Domain.Events;
using Mastery.Domain.Exceptions;
using Mastery.Domain.ValueObjects;

namespace Mastery.Domain.Entities.Experiment;

/// <summary>
/// Represents an experiment in the Mastery system.
/// An experiment tests a specific hypothesis by making a deliberate change
/// and observing the impact on a target metric over a defined time window.
/// </summary>
public sealed class Experiment : OwnedEntity, IAggregateRoot
{
    /// <summary>
    /// The title of the experiment.
    /// </summary>
    public string Title { get; private set; } = null!;

    /// <summary>
    /// Optional detailed description of the experiment.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// The category of change this experiment targets.
    /// </summary>
    public ExperimentCategory Category { get; private set; }

    /// <summary>
    /// Current lifecycle status of the experiment.
    /// </summary>
    public ExperimentStatus Status { get; private set; }

    /// <summary>
    /// How the experiment was originated (manual, weekly review, diagnostic, coaching).
    /// </summary>
    public ExperimentCreatedFrom CreatedFrom { get; private set; }

    /// <summary>
    /// The testable hypothesis for this experiment.
    /// </summary>
    public Hypothesis Hypothesis { get; private set; } = null!;

    /// <summary>
    /// How the experiment will be measured and evaluated.
    /// </summary>
    public MeasurementPlan MeasurementPlan { get; private set; } = null!;

    /// <summary>
    /// When the experiment run started.
    /// </summary>
    public DateOnly? StartDate { get; private set; }

    /// <summary>
    /// Planned end date for the experiment run.
    /// </summary>
    public DateOnly? EndDatePlanned { get; private set; }

    /// <summary>
    /// Actual end date (set on completion or abandonment).
    /// </summary>
    [EmbeddingIgnore]
    public DateOnly? EndDateActual { get; private set; }

    /// <summary>
    /// IDs of goals this experiment is linked to.
    /// </summary>
    private List<Guid> _linkedGoalIds = [];
    public IReadOnlyList<Guid> LinkedGoalIds => _linkedGoalIds.AsReadOnly();

    /// <summary>
    /// Notes captured during the experiment.
    /// </summary>
    private List<ExperimentNote> _notes = [];
    public IReadOnlyList<ExperimentNote> Notes => _notes.AsReadOnly();

    /// <summary>
    /// The measured result of the experiment (set on completion).
    /// </summary>
    [EmbeddingIgnore]
    public ExperimentResult? Result { get; private set; }

    private Experiment() { } // EF Core

    public static Experiment Create(
        string userId,
        string title,
        ExperimentCategory category,
        ExperimentCreatedFrom createdFrom,
        Hypothesis hypothesis,
        MeasurementPlan measurementPlan,
        string? description = null,
        IEnumerable<Guid>? linkedGoalIds = null,
        DateOnly? startDate = null,
        DateOnly? endDatePlanned = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("UserId cannot be empty.");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Experiment title cannot be empty.");

        if (title.Length > 200)
            throw new DomainException("Experiment title cannot exceed 200 characters.");

        if (description != null && description.Length > 2000)
            throw new DomainException("Experiment description cannot exceed 2000 characters.");

        if (hypothesis is null)
            throw new DomainException("Hypothesis is required.");

        if (measurementPlan is null)
            throw new DomainException("Measurement plan is required.");

        var experiment = new Experiment
        {
            UserId = userId,
            Title = title,
            Description = description,
            Category = category,
            Status = ExperimentStatus.Draft,
            CreatedFrom = createdFrom,
            Hypothesis = hypothesis,
            MeasurementPlan = measurementPlan,
            StartDate = startDate,
            EndDatePlanned = endDatePlanned,
            _linkedGoalIds = linkedGoalIds?.ToList() ?? []
        };

        experiment.AddDomainEvent(new ExperimentCreatedEvent(experiment.Id, userId, title));

        return experiment;
    }

    /// <summary>
    /// Updates experiment details. Only allowed in Draft status.
    /// Null parameters mean no change.
    /// </summary>
    public void Update(
        string? title = null,
        string? description = null,
        ExperimentCategory? category = null,
        Hypothesis? hypothesis = null,
        MeasurementPlan? measurementPlan = null,
        IEnumerable<Guid>? linkedGoalIds = null,
        DateOnly? startDate = null,
        DateOnly? endDatePlanned = null)
    {
        if (Status != ExperimentStatus.Draft)
            throw new DomainException("Only draft experiments can be updated.");

        if (title != null)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Experiment title cannot be empty.");
            if (title.Length > 200)
                throw new DomainException("Experiment title cannot exceed 200 characters.");
            Title = title;
        }

        if (description != null)
        {
            if (description.Length > 2000)
                throw new DomainException("Experiment description cannot exceed 2000 characters.");
            Description = description;
        }

        if (category.HasValue)
            Category = category.Value;

        if (hypothesis != null)
            Hypothesis = hypothesis;

        if (measurementPlan != null)
            MeasurementPlan = measurementPlan;

        if (linkedGoalIds != null)
            _linkedGoalIds = linkedGoalIds.ToList();

        if (startDate.HasValue)
            StartDate = startDate.Value;

        if (endDatePlanned.HasValue)
            EndDatePlanned = endDatePlanned.Value;
    }

    #region Status Transitions

    /// <summary>
    /// Starts the experiment. Transitions from Draft to Active.
    /// Sets StartDate to today if not already set.
    /// Computes EndDatePlanned from StartDate + RunWindowDays if not already set.
    /// </summary>
    public void Start()
    {
        if (Status != ExperimentStatus.Draft)
            throw new DomainException($"Cannot start an experiment with status {Status}. Only draft experiments can be started.");

        Status = ExperimentStatus.Active;

        StartDate ??= DateOnly.FromDateTime(DateTime.UtcNow);

        EndDatePlanned ??= StartDate.Value.AddDays(MeasurementPlan.RunWindowDays);

        AddDomainEvent(new ExperimentStartedEvent(Id, UserId));
    }

    /// <summary>
    /// Pauses the experiment. Transitions from Active to Paused.
    /// </summary>
    public void Pause()
    {
        if (Status != ExperimentStatus.Active)
            throw new DomainException("Only active experiments can be paused.");

        Status = ExperimentStatus.Paused;
        AddDomainEvent(new ExperimentPausedEvent(Id, UserId));
    }

    /// <summary>
    /// Resumes the experiment. Transitions from Paused to Active.
    /// </summary>
    public void Resume()
    {
        if (Status != ExperimentStatus.Paused)
            throw new DomainException("Only paused experiments can be resumed.");

        Status = ExperimentStatus.Active;
        AddDomainEvent(new ExperimentResumedEvent(Id, UserId));
    }

    /// <summary>
    /// Completes the experiment with a result. Transitions from Active or Paused to Completed.
    /// </summary>
    public void Complete(ExperimentResult result)
    {
        if (Status != ExperimentStatus.Active && Status != ExperimentStatus.Paused)
            throw new DomainException("Only active or paused experiments can be completed.");

        if (result is null)
            throw new DomainException("Experiment result is required for completion.");

        Status = ExperimentStatus.Completed;
        EndDateActual = DateOnly.FromDateTime(DateTime.UtcNow);
        Result = result;

        AddDomainEvent(new ExperimentCompletedEvent(Id, UserId, result.OutcomeClassification));
    }

    /// <summary>
    /// Abandons the experiment. Transitions from Active or Paused to Abandoned.
    /// </summary>
    public void Abandon(string? reason = null)
    {
        if (Status != ExperimentStatus.Active && Status != ExperimentStatus.Paused)
            throw new DomainException("Only active or paused experiments can be abandoned.");

        Status = ExperimentStatus.Abandoned;
        EndDateActual = DateOnly.FromDateTime(DateTime.UtcNow);

        AddDomainEvent(new ExperimentAbandonedEvent(Id, UserId, reason));
    }

    /// <summary>
    /// Archives the experiment. Transitions from Completed or Abandoned to Archived.
    /// </summary>
    public void Archive()
    {
        if (Status != ExperimentStatus.Completed && Status != ExperimentStatus.Abandoned)
            throw new DomainException("Only completed or abandoned experiments can be archived.");

        Status = ExperimentStatus.Archived;
    }

    #endregion

    #region Notes

    /// <summary>
    /// Adds a note to the experiment.
    /// </summary>
    public ExperimentNote AddNote(string content)
    {
        var note = ExperimentNote.Create(Id, content);
        _notes.Add(note);
        return note;
    }

    #endregion

    #region Query Helpers

    /// <summary>
    /// Whether the experiment is in Active status.
    /// </summary>
    public bool IsActive => Status == ExperimentStatus.Active;

    /// <summary>
    /// Whether the experiment is in Draft status.
    /// </summary>
    public bool IsDraft => Status == ExperimentStatus.Draft;

    /// <summary>
    /// Whether the experiment is currently running (Active or Paused).
    /// </summary>
    public bool IsRunning => Status == ExperimentStatus.Active || Status == ExperimentStatus.Paused;

    /// <summary>
    /// Number of days remaining until the planned end date.
    /// Returns null if no end date is planned or the experiment is not running.
    /// </summary>
    public int? DaysRemaining
    {
        get
        {
            if (EndDatePlanned is null)
                return null;

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var remaining = EndDatePlanned.Value.DayNumber - today.DayNumber;
            return remaining < 0 ? 0 : remaining;
        }
    }

    /// <summary>
    /// Number of days elapsed since the experiment started.
    /// Returns null if the experiment has not started.
    /// </summary>
    public int? DaysElapsed
    {
        get
        {
            if (StartDate is null)
                return null;

            var endDate = EndDateActual ?? DateOnly.FromDateTime(DateTime.UtcNow);
            return endDate.DayNumber - StartDate.Value.DayNumber;
        }
    }

    #endregion
}

using Mastery.Domain.Common;
using Mastery.Domain.Enums;
using Mastery.Domain.Events;
using Mastery.Domain.Exceptions;

namespace Mastery.Domain.Entities.Project;

/// <summary>
/// Represents a project in the Mastery system.
/// Projects are execution containers for achieving goals - grouping related tasks into a cohesive unit.
/// </summary>
public sealed class Project : OwnedEntity, IAggregateRoot
{
    #region Properties

    /// <summary>
    /// The title of the project.
    /// </summary>
    public string Title { get; private set; } = null!;

    /// <summary>
    /// Detailed description of the project.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Current lifecycle status of the project.
    /// </summary>
    public ProjectStatus Status { get; private set; }

    /// <summary>
    /// Priority level (1 = highest, 5 = lowest).
    /// </summary>
    public int Priority { get; private set; }

    /// <summary>
    /// Optional goal this project contributes to.
    /// </summary>
    public Guid? GoalId { get; private set; }

    /// <summary>
    /// Optional season this project is associated with.
    /// </summary>
    public Guid? SeasonId { get; private set; }

    /// <summary>
    /// Target end date for the project.
    /// </summary>
    public DateOnly? TargetEndDate { get; private set; }

    /// <summary>
    /// The next task to work on (single focus).
    /// </summary>
    public Guid? NextTaskId { get; private set; }

    /// <summary>
    /// IDs of roles this project is associated with.
    /// </summary>
    private List<Guid> _roleIds = [];
    public IReadOnlyList<Guid> RoleIds => _roleIds.AsReadOnly();

    /// <summary>
    /// IDs of values this project aligns with.
    /// </summary>
    private List<Guid> _valueIds = [];
    public IReadOnlyList<Guid> ValueIds => _valueIds.AsReadOnly();

    /// <summary>
    /// Milestones within this project.
    /// </summary>
    private List<Milestone> _milestones = [];
    public IReadOnlyList<Milestone> Milestones => _milestones.AsReadOnly();

    /// <summary>
    /// Notes about the outcome when project is completed.
    /// </summary>
    public string? OutcomeNotes { get; private set; }

    /// <summary>
    /// When the project was completed.
    /// </summary>
    [EmbeddingIgnore]
    public DateTime? CompletedAtUtc { get; private set; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Whether the project has any milestones.
    /// </summary>
    public bool HasMilestones => _milestones.Count > 0;

    /// <summary>
    /// Number of completed milestones.
    /// </summary>
    public int CompletedMilestonesCount => _milestones.Count(m => m.Status == MilestoneStatus.Completed);

    /// <summary>
    /// Whether the project is active and should have a next action.
    /// </summary>
    public bool ShouldHaveNextAction => Status == ProjectStatus.Active;

    /// <summary>
    /// Whether the project is "stuck" (active but no next action set).
    /// </summary>
    public bool IsStuck => Status == ProjectStatus.Active && NextTaskId == null;

    #endregion

    private Project() { } // EF Core

    #region Factory

    public static Project Create(
        string userId,
        string title,
        string? description = null,
        int priority = 3,
        Guid? goalId = null,
        Guid? seasonId = null,
        DateOnly? targetEndDate = null,
        IEnumerable<Guid>? roleIds = null,
        IEnumerable<Guid>? valueIds = null,
        bool saveAsDraft = false)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("UserId cannot be empty.");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Project title cannot be empty.");

        if (title.Length > 200)
            throw new DomainException("Project title cannot exceed 200 characters.");

        if (priority < 1 || priority > 5)
            throw new DomainException("Priority must be between 1 and 5.");

        var project = new Project
        {
            UserId = userId,
            Title = title,
            Description = description,
            Status = saveAsDraft ? ProjectStatus.Draft : ProjectStatus.Active,
            Priority = priority,
            GoalId = goalId,
            SeasonId = seasonId,
            TargetEndDate = targetEndDate,
            _roleIds = roleIds?.ToList() ?? [],
            _valueIds = valueIds?.ToList() ?? []
        };

        project.AddDomainEvent(new ProjectCreatedEvent(project.Id, userId, title));

        return project;
    }

    #endregion

    #region Core Updates

    public void Update(
        string? title = null,
        string? description = null,
        int? priority = null,
        Guid? goalId = null,
        Guid? seasonId = null,
        DateOnly? targetEndDate = null,
        IEnumerable<Guid>? roleIds = null,
        IEnumerable<Guid>? valueIds = null)
    {
        EnsureNotCompleted();
        EnsureNotArchived();

        if (title != null)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Project title cannot be empty.");
            if (title.Length > 200)
                throw new DomainException("Project title cannot exceed 200 characters.");
            Title = title;
        }

        if (description != null)
            Description = description;

        if (priority.HasValue)
        {
            if (priority.Value < 1 || priority.Value > 5)
                throw new DomainException("Priority must be between 1 and 5.");
            Priority = priority.Value;
        }

        // Allow setting to null or a new value
        GoalId = goalId;
        SeasonId = seasonId;

        if (targetEndDate.HasValue)
            TargetEndDate = targetEndDate;

        if (roleIds != null)
            _roleIds = roleIds.ToList();

        if (valueIds != null)
            _valueIds = valueIds.ToList();

        AddDomainEvent(new ProjectUpdatedEvent(Id, "Details"));
    }

    public void ClearTargetEndDate()
    {
        EnsureNotCompleted();
        EnsureNotArchived();

        TargetEndDate = null;
        AddDomainEvent(new ProjectUpdatedEvent(Id, "TargetEndDate"));
    }

    #endregion

    #region Status Transitions

    /// <summary>
    /// Activates the project for active work.
    /// </summary>
    public void Activate()
    {
        if (Status == ProjectStatus.Active)
            throw new DomainException("Project is already active.");

        if (Status is ProjectStatus.Completed or ProjectStatus.Archived)
            throw new DomainException($"Cannot activate a project with status {Status}.");

        var oldStatus = Status;
        Status = ProjectStatus.Active;

        AddDomainEvent(new ProjectStatusChangedEvent(Id, UserId, Status, oldStatus));
    }

    /// <summary>
    /// Pauses the project temporarily.
    /// </summary>
    public void Pause()
    {
        if (Status != ProjectStatus.Active)
            throw new DomainException("Only active projects can be paused.");

        var oldStatus = Status;
        Status = ProjectStatus.Paused;

        AddDomainEvent(new ProjectStatusChangedEvent(Id, UserId, Status, oldStatus));
    }

    /// <summary>
    /// Resumes a paused project.
    /// </summary>
    public void Resume()
    {
        if (Status != ProjectStatus.Paused)
            throw new DomainException("Only paused projects can be resumed.");

        var oldStatus = Status;
        Status = ProjectStatus.Active;

        AddDomainEvent(new ProjectStatusChangedEvent(Id, UserId, Status, oldStatus));
    }

    /// <summary>
    /// Completes the project.
    /// </summary>
    public void Complete(string? outcomeNotes = null)
    {
        if (Status is ProjectStatus.Completed or ProjectStatus.Archived)
            throw new DomainException($"Cannot complete a project with status {Status}.");

        var oldStatus = Status;
        Status = ProjectStatus.Completed;
        OutcomeNotes = outcomeNotes;
        CompletedAtUtc = DateTime.UtcNow;
        NextTaskId = null;

        AddDomainEvent(new ProjectCompletedEvent(Id, UserId, outcomeNotes));
        AddDomainEvent(new ProjectStatusChangedEvent(Id, UserId, Status, oldStatus));
    }

    /// <summary>
    /// Archives the project (soft delete).
    /// </summary>
    public void Archive()
    {
        if (Status == ProjectStatus.Archived)
            throw new DomainException("Project is already archived.");

        var oldStatus = Status;
        Status = ProjectStatus.Archived;
        NextTaskId = null;

        AddDomainEvent(new ProjectStatusChangedEvent(Id, UserId, Status, oldStatus));
    }

    /// <summary>
    /// Reactivates a completed project.
    /// </summary>
    public void Reactivate()
    {
        if (Status != ProjectStatus.Completed)
            throw new DomainException("Only completed projects can be reactivated.");

        var oldStatus = Status;
        Status = ProjectStatus.Active;
        OutcomeNotes = null;
        CompletedAtUtc = null;

        AddDomainEvent(new ProjectStatusChangedEvent(Id, UserId, Status, oldStatus));
    }

    #endregion

    #region Next Action

    /// <summary>
    /// Sets the next task to work on for this project.
    /// </summary>
    public void SetNextTask(Guid taskId)
    {
        EnsureNotCompleted();
        EnsureNotArchived();

        if (taskId == Guid.Empty)
            throw new DomainException("TaskId cannot be empty.");

        var oldNextTaskId = NextTaskId;
        NextTaskId = taskId;

        AddDomainEvent(new ProjectNextActionSetEvent(Id, UserId, taskId, oldNextTaskId));
    }

    /// <summary>
    /// Clears the next task (e.g., when the task is completed).
    /// </summary>
    public void ClearNextTask()
    {
        EnsureNotCompleted();
        EnsureNotArchived();

        if (NextTaskId.HasValue)
        {
            var oldNextTaskId = NextTaskId;
            NextTaskId = null;
            AddDomainEvent(new ProjectNextActionSetEvent(Id, UserId, null, oldNextTaskId));
        }
    }

    #endregion

    #region Milestones

    public Milestone AddMilestone(
        string title,
        DateOnly? targetDate = null,
        string? notes = null,
        int? displayOrder = null)
    {
        EnsureNotCompleted();
        EnsureNotArchived();

        var order = displayOrder ?? (_milestones.Count > 0 ? _milestones.Max(m => m.DisplayOrder) + 1 : 0);

        var milestone = Milestone.Create(Id, title, targetDate, notes, order);
        _milestones.Add(milestone);

        AddDomainEvent(new MilestoneAddedEvent(Id, milestone.Id, title));

        return milestone;
    }

    public void UpdateMilestone(
        Guid milestoneId,
        string? title = null,
        DateOnly? targetDate = null,
        string? notes = null,
        int? displayOrder = null)
    {
        EnsureNotCompleted();
        EnsureNotArchived();

        var milestone = _milestones.FirstOrDefault(m => m.Id == milestoneId)
            ?? throw new DomainException("Milestone not found.");

        milestone.Update(title, targetDate, notes, displayOrder);
        AddDomainEvent(new ProjectUpdatedEvent(Id, "Milestone"));
    }

    public void CompleteMilestone(Guid milestoneId)
    {
        EnsureNotArchived();

        var milestone = _milestones.FirstOrDefault(m => m.Id == milestoneId)
            ?? throw new DomainException("Milestone not found.");

        milestone.Complete();
        AddDomainEvent(new MilestoneCompletedEvent(Id, milestoneId, milestone.Title));
    }

    public void RemoveMilestone(Guid milestoneId)
    {
        EnsureNotCompleted();
        EnsureNotArchived();

        var milestone = _milestones.FirstOrDefault(m => m.Id == milestoneId)
            ?? throw new DomainException("Milestone not found.");

        _milestones.Remove(milestone);
        AddDomainEvent(new ProjectUpdatedEvent(Id, "Milestone"));
    }

    public void ReorderMilestones(IEnumerable<Guid> orderedMilestoneIds)
    {
        EnsureNotCompleted();
        EnsureNotArchived();

        var orderedIds = orderedMilestoneIds.ToList();
        if (orderedIds.Count != _milestones.Count)
            throw new DomainException("Must provide exactly the same number of milestone IDs.");

        var order = 0;
        foreach (var milestoneId in orderedIds)
        {
            var milestone = _milestones.FirstOrDefault(m => m.Id == milestoneId)
                ?? throw new DomainException($"Milestone {milestoneId} not found.");

            milestone.Update(displayOrder: order);
            order++;
        }

        AddDomainEvent(new ProjectUpdatedEvent(Id, "MilestoneOrder"));
    }

    #endregion

    #region Private Helpers

    private void EnsureNotCompleted()
    {
        if (Status == ProjectStatus.Completed)
            throw new DomainException("Cannot modify a completed project.");
    }

    private void EnsureNotArchived()
    {
        if (Status == ProjectStatus.Archived)
            throw new DomainException("Cannot modify an archived project.");
    }

    #endregion
}

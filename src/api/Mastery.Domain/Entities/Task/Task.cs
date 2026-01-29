using Mastery.Domain.Common;
using Mastery.Domain.Enums;
using Mastery.Domain.Events;
using Mastery.Domain.Exceptions;
using Mastery.Domain.ValueObjects;
using TaskStatus = Mastery.Domain.Enums.TaskStatus;

namespace Mastery.Domain.Entities.Task;

/// <summary>
/// Represents a task in the Mastery system.
/// Tasks are the primary "actuators" in the control loop - converting user intentions into discrete execution steps.
/// </summary>
public sealed class Task : OwnedEntity, IAggregateRoot
{
    #region Properties

    /// <summary>
    /// Optional project this task belongs to.
    /// </summary>
    public Guid? ProjectId { get; private set; }

    /// <summary>
    /// Optional goal this task directly contributes to.
    /// </summary>
    public Guid? GoalId { get; private set; }

    /// <summary>
    /// The title of the task.
    /// </summary>
    public string Title { get; private set; } = null!;

    /// <summary>
    /// Detailed description or notes for the task.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Current lifecycle status of the task.
    /// </summary>
    public TaskStatus Status { get; private set; }

    /// <summary>
    /// Priority level (1 = highest, 5 = lowest).
    /// </summary>
    public int Priority { get; private set; }

    /// <summary>
    /// Estimated minutes to complete the task.
    /// </summary>
    public int EstimatedMinutes { get; private set; }

    /// <summary>
    /// Energy cost level (1 = lowest, 5 = highest).
    /// Used for NBA ranking based on current energy.
    /// </summary>
    public int EnergyCost { get; private set; }

    /// <summary>
    /// Display order for sorting in lists.
    /// </summary>
    [EmbeddingIgnore]
    public int DisplayOrder { get; private set; }

    /// <summary>
    /// Due date configuration for the task.
    /// </summary>
    public TaskDue? Due { get; private set; }

    /// <summary>
    /// Scheduling configuration for the task.
    /// </summary>
    public TaskScheduling? Scheduling { get; private set; }

    /// <summary>
    /// Completion data when the task is completed.
    /// </summary>
    public TaskCompletion? Completion { get; private set; }

    /// <summary>
    /// Context tags for NBA filtering.
    /// </summary>
    private List<ContextTag> _contextTags = [];
    public IReadOnlyList<ContextTag> ContextTags => _contextTags.AsReadOnly();

    /// <summary>
    /// IDs of tasks this task is blocked by.
    /// </summary>
    private List<Guid> _dependencyTaskIds = [];
    public IReadOnlyList<Guid> DependencyTaskIds => _dependencyTaskIds.AsReadOnly();

    /// <summary>
    /// IDs of roles this task is associated with.
    /// </summary>
    private List<Guid> _roleIds = [];
    public IReadOnlyList<Guid> RoleIds => _roleIds.AsReadOnly();

    /// <summary>
    /// IDs of values this task aligns with.
    /// </summary>
    private List<Guid> _valueIds = [];
    public IReadOnlyList<Guid> ValueIds => _valueIds.AsReadOnly();

    /// <summary>
    /// Metric bindings - how this task contributes to metrics.
    /// </summary>
    private List<TaskMetricBinding> _metricBindings = [];
    public IReadOnlyList<TaskMetricBinding> MetricBindings => _metricBindings.AsReadOnly();

    /// <summary>
    /// The reason for the last reschedule (for friction analysis).
    /// </summary>
    public RescheduleReason? LastRescheduleReason { get; private set; }

    /// <summary>
    /// Number of times this task has been rescheduled.
    /// </summary>
    [EmbeddingIgnore]
    public int RescheduleCount { get; private set; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Whether the task has unresolved dependencies.
    /// Note: Full blocked check requires querying dependency task statuses.
    /// </summary>
    public bool HasDependencies => _dependencyTaskIds.Count > 0;

    /// <summary>
    /// Whether the task has a hard due date that has passed.
    /// </summary>
    public bool IsOverdue => Due?.IsOverdue(DateOnly.FromDateTime(DateTime.UtcNow)) ?? false;

    /// <summary>
    /// Whether the task is eligible for Next Best Action ranking.
    /// </summary>
    public bool IsEligibleForNBA => Status is TaskStatus.Ready or TaskStatus.Scheduled;

    /// <summary>
    /// Whether the task has metric bindings.
    /// </summary>
    public bool HasMetricBindings => _metricBindings.Count > 0;

    /// <summary>
    /// Whether this task requires value entry at completion.
    /// </summary>
    public bool RequiresValueEntry => _metricBindings.Any(b =>
        b.ContributionType == TaskContributionType.UseEnteredValue);

    #endregion

    private Task() { } // EF Core

    #region Factory

    public static Task Create(
        string userId,
        string title,
        int estimatedMinutes = 30,
        int energyCost = 3,
        string? description = null,
        int priority = 3,
        Guid? projectId = null,
        Guid? goalId = null,
        TaskDue? due = null,
        TaskScheduling? scheduling = null,
        IEnumerable<ContextTag>? contextTags = null,
        IEnumerable<Guid>? dependencyTaskIds = null,
        IEnumerable<Guid>? roleIds = null,
        IEnumerable<Guid>? valueIds = null,
        int displayOrder = 0,
        bool startAsReady = false)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("UserId cannot be empty.");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Task title cannot be empty.");

        if (title.Length > 200)
            throw new DomainException("Task title cannot exceed 200 characters.");

        if (estimatedMinutes < 1)
            throw new DomainException("Estimated minutes must be at least 1.");

        if (estimatedMinutes > 480)
            throw new DomainException("Estimated minutes cannot exceed 480 (8 hours).");

        if (energyCost < 1 || energyCost > 5)
            throw new DomainException("Energy cost must be between 1 and 5.");

        if (priority < 1 || priority > 5)
            throw new DomainException("Priority must be between 1 and 5.");

        // Determine initial status
        var initialStatus = scheduling != null
            ? TaskStatus.Scheduled
            : startAsReady ? TaskStatus.Ready : TaskStatus.Inbox;

        var task = new Task
        {
            UserId = userId,
            Title = title,
            Description = description,
            Status = initialStatus,
            Priority = priority,
            EstimatedMinutes = estimatedMinutes,
            EnergyCost = energyCost,
            DisplayOrder = displayOrder,
            ProjectId = projectId,
            GoalId = goalId,
            Due = due,
            Scheduling = scheduling,
            _contextTags = contextTags?.ToList() ?? [],
            _dependencyTaskIds = dependencyTaskIds?.ToList() ?? [],
            _roleIds = roleIds?.ToList() ?? [],
            _valueIds = valueIds?.ToList() ?? [],
            RescheduleCount = 0
        };

        task.AddDomainEvent(new TaskCreatedEvent(task.Id, userId, title));

        return task;
    }

    #endregion

    #region Core Updates

    public void Update(
        string? title = null,
        string? description = null,
        int? priority = null,
        int? estimatedMinutes = null,
        int? energyCost = null,
        int? displayOrder = null,
        Guid? projectId = null,
        Guid? goalId = null,
        TaskDue? due = null,
        IEnumerable<ContextTag>? contextTags = null,
        IEnumerable<Guid>? roleIds = null,
        IEnumerable<Guid>? valueIds = null)
    {
        EnsureNotCompleted();
        EnsureNotArchived();

        if (title != null)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Task title cannot be empty.");
            if (title.Length > 200)
                throw new DomainException("Task title cannot exceed 200 characters.");
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

        if (estimatedMinutes.HasValue)
        {
            if (estimatedMinutes.Value < 1 || estimatedMinutes.Value > 480)
                throw new DomainException("Estimated minutes must be between 1 and 480.");
            EstimatedMinutes = estimatedMinutes.Value;
        }

        if (energyCost.HasValue)
        {
            if (energyCost.Value < 1 || energyCost.Value > 5)
                throw new DomainException("Energy cost must be between 1 and 5.");
            EnergyCost = energyCost.Value;
        }

        if (displayOrder.HasValue)
            DisplayOrder = displayOrder.Value;

        // Allow setting to null or a new value
        ProjectId = projectId;
        GoalId = goalId;

        if (due != null)
            Due = due;

        if (contextTags != null)
            _contextTags = contextTags.ToList();

        if (roleIds != null)
            _roleIds = roleIds.ToList();

        if (valueIds != null)
            _valueIds = valueIds.ToList();

        AddDomainEvent(new TaskUpdatedEvent(Id, "Details"));
    }

    public void ClearDue()
    {
        EnsureNotCompleted();
        EnsureNotArchived();

        Due = null;
        AddDomainEvent(new TaskUpdatedEvent(Id, "Due"));
    }

    #endregion

    #region Status Transitions

    /// <summary>
    /// Moves the task from Inbox to Ready status.
    /// </summary>
    public void MoveToReady()
    {
        if (Status != TaskStatus.Inbox)
            throw new DomainException($"Only Inbox tasks can be moved to Ready. Current status: {Status}");

        var oldStatus = Status;
        Status = TaskStatus.Ready;
        Scheduling = null; // Clear any scheduling

        AddDomainEvent(new TaskStatusChangedEvent(Id, UserId, Status, oldStatus));
    }

    /// <summary>
    /// Schedules the task for a specific date.
    /// </summary>
    public void Schedule(DateOnly scheduledOn, TimeWindow? preferredTimeWindow = null)
    {
        if (Status is TaskStatus.Completed or TaskStatus.Cancelled or TaskStatus.Archived)
            throw new DomainException($"Cannot schedule a task with status {Status}.");

        var oldStatus = Status;
        Status = TaskStatus.Scheduled;
        Scheduling = TaskScheduling.Create(scheduledOn, preferredTimeWindow);

        AddDomainEvent(new TaskScheduledEvent(Id, UserId, scheduledOn));
        if (oldStatus != TaskStatus.Scheduled)
        {
            AddDomainEvent(new TaskStatusChangedEvent(Id, UserId, Status, oldStatus));
        }
    }

    /// <summary>
    /// Reschedules the task to a new date.
    /// </summary>
    public void Reschedule(DateOnly newDate, RescheduleReason? reason = null)
    {
        if (Status is TaskStatus.Completed or TaskStatus.Cancelled or TaskStatus.Archived)
            throw new DomainException($"Cannot reschedule a task with status {Status}.");

        var oldDate = Scheduling?.ScheduledOn ?? DateOnly.FromDateTime(DateTime.UtcNow);
        Scheduling = TaskScheduling.Create(newDate, Scheduling?.PreferredTimeWindow);

        if (Status != TaskStatus.Scheduled)
        {
            var oldStatus = Status;
            Status = TaskStatus.Scheduled;
            AddDomainEvent(new TaskStatusChangedEvent(Id, UserId, Status, oldStatus));
        }

        if (reason.HasValue)
            LastRescheduleReason = reason;

        RescheduleCount++;

        AddDomainEvent(new TaskRescheduledEvent(Id, UserId, oldDate, newDate, reason));
    }

    /// <summary>
    /// Marks the task as in progress.
    /// </summary>
    public void StartProgress()
    {
        if (Status is not (TaskStatus.Ready or TaskStatus.Scheduled))
            throw new DomainException($"Only Ready or Scheduled tasks can be started. Current status: {Status}");

        var oldStatus = Status;
        Status = TaskStatus.InProgress;

        AddDomainEvent(new TaskStatusChangedEvent(Id, UserId, Status, oldStatus));
    }

    /// <summary>
    /// Completes the task.
    /// </summary>
    public void Complete(
        DateOnly completedOn,
        int? actualMinutes = null,
        string? note = null,
        decimal? enteredValue = null)
    {
        if (Status is TaskStatus.Completed or TaskStatus.Cancelled or TaskStatus.Archived)
            throw new DomainException($"Cannot complete a task with status {Status}.");

        if (RequiresValueEntry && !enteredValue.HasValue)
            throw new DomainException("A value must be entered for this task's metric binding.");

        var oldStatus = Status;
        Status = TaskStatus.Completed;
        Completion = TaskCompletion.Now(completedOn, actualMinutes, note, enteredValue);

        AddDomainEvent(new TaskCompletedEvent(Id, UserId, completedOn, actualMinutes, enteredValue));
        AddDomainEvent(new TaskStatusChangedEvent(Id, UserId, Status, oldStatus));
    }

    /// <summary>
    /// Undoes the completion and returns to Ready status.
    /// </summary>
    public void UndoCompletion()
    {
        if (Status != TaskStatus.Completed)
            throw new DomainException("Only completed tasks can be undone.");

        var oldStatus = Status;
        Status = TaskStatus.Ready;
        Completion = null;

        AddDomainEvent(new TaskCompletionUndoneEvent(Id, UserId));
        AddDomainEvent(new TaskStatusChangedEvent(Id, UserId, Status, oldStatus));
    }

    /// <summary>
    /// Cancels the task.
    /// </summary>
    public void Cancel()
    {
        if (Status is TaskStatus.Completed or TaskStatus.Archived)
            throw new DomainException($"Cannot cancel a task with status {Status}.");

        var oldStatus = Status;
        Status = TaskStatus.Cancelled;

        AddDomainEvent(new TaskCancelledEvent(Id, UserId));
        AddDomainEvent(new TaskStatusChangedEvent(Id, UserId, Status, oldStatus));
    }

    /// <summary>
    /// Archives the task (soft delete).
    /// </summary>
    public void Archive()
    {
        if (Status == TaskStatus.Archived)
            throw new DomainException("Task is already archived.");

        var oldStatus = Status;
        Status = TaskStatus.Archived;

        AddDomainEvent(new TaskArchivedEvent(Id, UserId));
    }

    #endregion

    #region Dependencies

    public void AddDependency(Guid dependencyTaskId)
    {
        EnsureNotCompleted();
        EnsureNotArchived();

        if (dependencyTaskId == Guid.Empty)
            throw new DomainException("Dependency task ID cannot be empty.");

        if (dependencyTaskId == Id)
            throw new DomainException("A task cannot depend on itself.");

        if (_dependencyTaskIds.Contains(dependencyTaskId))
            throw new DomainException("This dependency already exists.");

        _dependencyTaskIds.Add(dependencyTaskId);
        AddDomainEvent(new TaskDependencyAddedEvent(Id, dependencyTaskId));
    }

    public void RemoveDependency(Guid dependencyTaskId)
    {
        EnsureNotCompleted();
        EnsureNotArchived();

        if (!_dependencyTaskIds.Contains(dependencyTaskId))
            throw new DomainException("Dependency not found.");

        _dependencyTaskIds.Remove(dependencyTaskId);
        AddDomainEvent(new TaskDependencyRemovedEvent(Id, dependencyTaskId));
    }

    public void ClearDependencies()
    {
        EnsureNotCompleted();
        EnsureNotArchived();

        _dependencyTaskIds.Clear();
        AddDomainEvent(new TaskUpdatedEvent(Id, "Dependencies"));
    }

    #endregion

    #region Metric Bindings

    public TaskMetricBinding AddMetricBinding(
        Guid metricDefinitionId,
        TaskContributionType contributionType,
        decimal? fixedValue = null,
        string? notes = null)
    {
        EnsureNotCompleted();
        EnsureNotArchived();

        if (metricDefinitionId == Guid.Empty)
            throw new DomainException("MetricDefinitionId cannot be empty.");

        if (_metricBindings.Any(b => b.MetricDefinitionId == metricDefinitionId))
            throw new DomainException("This metric is already bound to this task.");

        if (contributionType == TaskContributionType.FixedValue && !fixedValue.HasValue)
            throw new DomainException("FixedValue is required when contribution type is FixedValue.");

        var binding = TaskMetricBinding.Create(
            Id,
            metricDefinitionId,
            contributionType,
            fixedValue,
            notes);

        _metricBindings.Add(binding);
        AddDomainEvent(new TaskUpdatedEvent(Id, "MetricBinding"));

        return binding;
    }

    public void UpdateMetricBinding(
        Guid bindingId,
        TaskContributionType contributionType,
        decimal? fixedValue = null,
        string? notes = null)
    {
        EnsureNotCompleted();
        EnsureNotArchived();

        var binding = _metricBindings.FirstOrDefault(b => b.Id == bindingId)
            ?? throw new DomainException("Metric binding not found.");

        binding.Update(contributionType, fixedValue, notes);
        AddDomainEvent(new TaskUpdatedEvent(Id, "MetricBinding"));
    }

    public void RemoveMetricBinding(Guid bindingId)
    {
        EnsureNotCompleted();
        EnsureNotArchived();

        var binding = _metricBindings.FirstOrDefault(b => b.Id == bindingId)
            ?? throw new DomainException("Metric binding not found.");

        _metricBindings.Remove(binding);
        AddDomainEvent(new TaskUpdatedEvent(Id, "MetricBinding"));
    }

    #endregion

    #region Private Helpers

    private void EnsureNotCompleted()
    {
        if (Status == TaskStatus.Completed)
            throw new DomainException("Cannot modify a completed task.");
    }

    private void EnsureNotArchived()
    {
        if (Status == TaskStatus.Archived)
            throw new DomainException("Cannot modify an archived task.");
    }

    #endregion
}

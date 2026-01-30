using Mastery.Domain.Common;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.ValueObjects;

namespace Mastery.Domain.Entities.Goal;

/// <summary>
/// Represents a goal in the Mastery system.
/// A goal is the "setpoint" in the control loop - what the user is trying to achieve.
/// </summary>
public sealed class Goal : OwnedEntity, IAggregateRoot
{
    /// <summary>
    /// The title of the goal.
    /// </summary>
    public string Title { get; private set; } = null!;

    /// <summary>
    /// Detailed description of the goal.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// The "why" behind the goal - motivation and purpose.
    /// </summary>
    public string? Why { get; private set; }

    /// <summary>
    /// Current lifecycle status of the goal.
    /// </summary>
    public GoalStatus Status { get; private set; }

    /// <summary>
    /// Priority level (1-5, where 1 is highest priority).
    /// </summary>
    public int Priority { get; private set; }

    /// <summary>
    /// Target deadline for achieving the goal.
    /// </summary>
    public DateOnly? Deadline { get; private set; }

    /// <summary>
    /// Associated season (if any).
    /// </summary>
    public Guid? SeasonId { get; private set; }

    /// <summary>
    /// IDs of roles this goal is associated with.
    /// </summary>
    private List<Guid> _roleIds = [];
    public IReadOnlyList<Guid> RoleIds => _roleIds.AsReadOnly();

    /// <summary>
    /// IDs of values this goal aligns with.
    /// </summary>
    private List<Guid> _valueIds = [];
    public IReadOnlyList<Guid> ValueIds => _valueIds.AsReadOnly();

    /// <summary>
    /// IDs of goals this goal depends on.
    /// </summary>
    private List<Guid> _dependencyIds = [];
    public IReadOnlyList<Guid> DependencyIds => _dependencyIds.AsReadOnly();

    /// <summary>
    /// The scoreboard - metrics attached to this goal.
    /// </summary>
    private List<GoalMetric> _metrics = [];
    public IReadOnlyList<GoalMetric> Metrics => _metrics.AsReadOnly();

    /// <summary>
    /// Completion notes (set when goal is completed).
    /// </summary>
    public string? CompletionNotes { get; private set; }

    /// <summary>
    /// When the goal was completed (if applicable).
    /// </summary>
    [EmbeddingIgnore]
    public DateTime? CompletedAt { get; private set; }

    private Goal() { } // EF Core

    public static Goal Create(
        string userId,
        string title,
        string? description = null,
        string? why = null,
        int priority = 3,
        DateOnly? deadline = null,
        Guid? seasonId = null,
        IEnumerable<Guid>? roleIds = null,
        IEnumerable<Guid>? valueIds = null,
        IEnumerable<Guid>? dependencyIds = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("UserId cannot be empty.");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Goal title cannot be empty.");

        if (title.Length > 200)
            throw new DomainException("Goal title cannot exceed 200 characters.");

        if (priority < 1 || priority > 5)
            throw new DomainException("Priority must be between 1 and 5.");

        var goal = new Goal
        {
            UserId = userId,
            Title = title,
            Description = description,
            Why = why,
            Status = GoalStatus.Draft,
            Priority = priority,
            Deadline = deadline,
            SeasonId = seasonId,
            _roleIds = roleIds?.ToList() ?? [],
            _valueIds = valueIds?.ToList() ?? [],
            _dependencyIds = dependencyIds?.ToList() ?? []
        };

        goal.AddDomainEvent(new GoalCreatedEvent(goal.Id, userId, title));

        return goal;
    }

    public void Update(
        string? title = null,
        string? description = null,
        string? why = null,
        int? priority = null,
        DateOnly? deadline = null,
        Guid? seasonId = null,
        IEnumerable<Guid>? roleIds = null,
        IEnumerable<Guid>? valueIds = null,
        IEnumerable<Guid>? dependencyIds = null)
    {
        if (Status == GoalStatus.Completed || Status == GoalStatus.Archived)
            throw new DomainException("Cannot update a completed or archived goal.");

        if (title != null)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Goal title cannot be empty.");
            if (title.Length > 200)
                throw new DomainException("Goal title cannot exceed 200 characters.");
            Title = title;
        }

        if (description != null)
            Description = description;

        if (why != null)
            Why = why;

        if (priority.HasValue)
        {
            if (priority.Value < 1 || priority.Value > 5)
                throw new DomainException("Priority must be between 1 and 5.");
            Priority = priority.Value;
        }

        // Deadline can be set to null to remove it
        Deadline = deadline ?? Deadline;

        // SeasonId can be set to null to remove association
        SeasonId = seasonId ?? SeasonId;

        if (roleIds != null)
            _roleIds = roleIds.ToList();

        if (valueIds != null)
            _valueIds = valueIds.ToList();

        if (dependencyIds != null)
            _dependencyIds = dependencyIds.ToList();

        AddDomainEvent(new GoalUpdatedEvent(Id, "Details"));
    }

    #region Status Transitions

    public void Activate()
    {
        if (Status != GoalStatus.Draft && Status != GoalStatus.Paused)
            throw new DomainException($"Cannot activate a goal with status {Status}.");

        Status = GoalStatus.Active;
        AddDomainEvent(new GoalStatusChangedEvent(Id, UserId, Status));
    }

    public void Pause()
    {
        if (Status != GoalStatus.Active)
            throw new DomainException("Only active goals can be paused.");

        Status = GoalStatus.Paused;
        AddDomainEvent(new GoalStatusChangedEvent(Id, UserId, Status));
    }

    public void Resume()
    {
        if (Status != GoalStatus.Paused)
            throw new DomainException("Only paused goals can be resumed.");

        Status = GoalStatus.Active;
        AddDomainEvent(new GoalStatusChangedEvent(Id, UserId, Status));
    }

    public void Complete(string? notes = null)
    {
        if (Status != GoalStatus.Active && Status != GoalStatus.Paused)
            throw new DomainException("Only active or paused goals can be completed.");

        Status = GoalStatus.Completed;
        CompletionNotes = notes;
        CompletedAt = DateTime.UtcNow;

        AddDomainEvent(new GoalCompletedEvent(Id, UserId, notes));
    }

    public void Archive()
    {
        if (Status == GoalStatus.Archived)
            throw new DomainException("Goal is already archived.");

        Status = GoalStatus.Archived;
        AddDomainEvent(new GoalStatusChangedEvent(Id, UserId, Status));
    }

    #endregion

    #region Scoreboard Management

    public GoalMetric AddMetric(
        Guid metricDefinitionId,
        MetricKind kind,
        Target target,
        EvaluationWindow evaluationWindow,
        MetricAggregation aggregation,
        decimal weight = 1.0m,
        MetricSourceType sourceHint = MetricSourceType.Manual,
        decimal? baseline = null,
        decimal? minimumThreshold = null)
    {
        // Validate scoreboard constraints (soft constraints - warnings, not hard fails)
        // For now, we allow multiple lag metrics but the system will warn
        if (kind == MetricKind.Lag && _metrics.Count(m => m.Kind == MetricKind.Lag) >= 1)
        {
            // Allow but could emit a warning event
        }

        // Check if this metric definition is already in the scoreboard
        if (_metrics.Any(m => m.MetricDefinitionId == metricDefinitionId))
            throw new DomainException("This metric is already in the goal's scoreboard.");

        var displayOrder = _metrics.Count;

        var metric = GoalMetric.Create(
            goalId: Id,
            metricDefinitionId: metricDefinitionId,
            kind: kind,
            target: target,
            evaluationWindow: evaluationWindow,
            aggregation: aggregation,
            weight: weight,
            sourceHint: sourceHint,
            displayOrder: displayOrder,
            baseline: baseline,
            minimumThreshold: minimumThreshold);

        _metrics.Add(metric);

        AddDomainEvent(new GoalScoreboardUpdatedEvent(Id, "MetricAdded", metricDefinitionId));

        return metric;
    }

    public void RemoveMetric(Guid metricId)
    {
        var metric = _metrics.FirstOrDefault(m => m.Id == metricId)
            ?? throw new DomainException("Metric not found in scoreboard.");

        _metrics.Remove(metric);

        // Reorder remaining metrics
        for (int i = 0; i < _metrics.Count; i++)
        {
            _metrics[i].UpdateDisplayOrder(i);
        }

        AddDomainEvent(new GoalScoreboardUpdatedEvent(Id, "MetricRemoved", metric.MetricDefinitionId));
    }

    public void ReorderMetrics(IEnumerable<Guid> metricIdsInOrder)
    {
        var orderedIds = metricIdsInOrder.ToList();

        if (orderedIds.Count != _metrics.Count)
            throw new DomainException("Must provide all metric IDs for reordering.");

        if (orderedIds.Distinct().Count() != orderedIds.Count)
            throw new DomainException("Duplicate metric IDs in reorder list.");

        for (int i = 0; i < orderedIds.Count; i++)
        {
            var metric = _metrics.FirstOrDefault(m => m.Id == orderedIds[i])
                ?? throw new DomainException($"Metric {orderedIds[i]} not found in scoreboard.");

            metric.UpdateDisplayOrder(i);
        }

        AddDomainEvent(new GoalScoreboardUpdatedEvent(Id, "MetricsReordered", null));
    }

    public void UpdateMetrics(IEnumerable<GoalMetric> metrics)
    {
        // Clear and repopulate the SAME list to preserve EF Core's change tracking.
        // If we replace the list reference (_metrics = newList), EF Core loses track
        // of what was removed vs added, causing concurrency errors.
        _metrics.Clear();
        foreach (var metric in metrics)
        {
            _metrics.Add(metric);
        }
        AddDomainEvent(new GoalScoreboardUpdatedEvent(Id, "ScoreboardReplaced", null));
    }

    #endregion

    #region Query Helpers

    public GoalMetric? GetLagMetric() => _metrics.FirstOrDefault(m => m.Kind == MetricKind.Lag);

    public IEnumerable<GoalMetric> GetLeadMetrics() => _metrics.Where(m => m.Kind == MetricKind.Lead);

    public GoalMetric? GetConstraintMetric() => _metrics.FirstOrDefault(m => m.Kind == MetricKind.Constraint);

    public bool HasScoreboard => _metrics.Count > 0;

    public bool IsOverdue => Deadline.HasValue && Deadline.Value < DateOnly.FromDateTime(DateTime.UtcNow);

    public bool IsActive => Status == GoalStatus.Active;

    #endregion
}

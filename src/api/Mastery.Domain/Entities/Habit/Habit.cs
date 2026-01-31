using Mastery.Domain.Common;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.ValueObjects;

namespace Mastery.Domain.Entities.Habit;

/// <summary>
/// Represents a habit in the Mastery system.
/// Habits are the primary "sensors" for lead metrics - they generate observations when completed.
/// </summary>
public sealed class Habit : OwnedEntity, IAggregateRoot
{
    /// <summary>
    /// The title of the habit.
    /// </summary>
    public string Title { get; private set; } = null!;

    /// <summary>
    /// Detailed description of the habit.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// The "why" behind the habit - motivation and purpose for coaching.
    /// </summary>
    public string? Why { get; private set; }

    /// <summary>
    /// Current lifecycle status of the habit.
    /// </summary>
    public HabitStatus Status { get; private set; }

    /// <summary>
    /// Display order for sorting in lists.
    /// </summary>
    [EmbeddingIgnore]
    public int DisplayOrder { get; private set; }

    /// <summary>
    /// The schedule configuration for this habit.
    /// </summary>
    public HabitSchedule Schedule { get; private set; } = null!;

    /// <summary>
    /// The policy rules for this habit (late completion, skipping, etc.).
    /// </summary>
    public HabitPolicy Policy { get; private set; } = null!;

    /// <summary>
    /// The default execution mode for this habit.
    /// </summary>
    public HabitMode DefaultMode { get; private set; }

    /// <summary>
    /// IDs of roles this habit is associated with.
    /// </summary>
    private List<Guid> _roleIds = [];
    public IReadOnlyList<Guid> RoleIds => _roleIds.AsReadOnly();

    /// <summary>
    /// IDs of values this habit aligns with.
    /// </summary>
    private List<Guid> _valueIds = [];
    public IReadOnlyList<Guid> ValueIds => _valueIds.AsReadOnly();

    /// <summary>
    /// IDs of goals this habit contributes to (via metric bindings).
    /// </summary>
    private List<Guid> _goalIds = [];
    public IReadOnlyList<Guid> GoalIds => _goalIds.AsReadOnly();

    /// <summary>
    /// Metric bindings - how this habit contributes to metrics.
    /// </summary>
    private List<HabitMetricBinding> _metricBindings = [];
    public IReadOnlyList<HabitMetricBinding> MetricBindings => _metricBindings.AsReadOnly();

    /// <summary>
    /// Mode variants - Full/Maintenance/Minimum versions.
    /// </summary>
    private List<HabitVariant> _variants = [];
    public IReadOnlyList<HabitVariant> Variants => _variants.AsReadOnly();

    /// <summary>
    /// Occurrences - individual scheduled instances of the habit.
    /// </summary>
    private List<HabitOccurrence> _occurrences = [];
    public IReadOnlyList<HabitOccurrence> Occurrences => _occurrences.AsReadOnly();

    /// <summary>
    /// Current streak count (computed, for projections).
    /// </summary>
    public int CurrentStreak { get; private set; }

    /// <summary>
    /// 7-day adherence rate (computed, for projections).
    /// </summary>
    public decimal AdherenceRate7Day { get; private set; }

    private Habit() { } // EF Core

    public static Habit Create(
        string userId,
        string title,
        HabitSchedule schedule,
        string? description = null,
        string? why = null,
        HabitPolicy? policy = null,
        HabitMode defaultMode = HabitMode.Full,
        int displayOrder = 0,
        IEnumerable<Guid>? roleIds = null,
        IEnumerable<Guid>? valueIds = null,
        IEnumerable<Guid>? goalIds = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("UserId cannot be empty.");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Habit title cannot be empty.");

        if (title.Length > 200)
            throw new DomainException("Habit title cannot exceed 200 characters.");

        var habit = new Habit
        {
            UserId = userId,
            Title = title,
            Description = description,
            Why = why,
            Status = HabitStatus.Active,
            DisplayOrder = displayOrder,
            Schedule = schedule ?? throw new DomainException("Schedule is required."),
            Policy = policy ?? HabitPolicy.Default(),
            DefaultMode = defaultMode,
            _roleIds = roleIds?.ToList() ?? [],
            _valueIds = valueIds?.ToList() ?? [],
            _goalIds = goalIds?.ToList() ?? [],
            CurrentStreak = 0,
            AdherenceRate7Day = 0
        };

        habit.AddDomainEvent(new HabitCreatedEvent(habit.Id, userId, title));

        return habit;
    }

    #region Core Updates

    public void Update(
        string? title = null,
        string? description = null,
        string? why = null,
        int? displayOrder = null,
        IEnumerable<Guid>? roleIds = null,
        IEnumerable<Guid>? valueIds = null,
        IEnumerable<Guid>? goalIds = null)
    {
        if (Status == HabitStatus.Archived)
            throw new DomainException("Cannot update an archived habit.");

        if (title != null)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new DomainException("Habit title cannot be empty.");
            if (title.Length > 200)
                throw new DomainException("Habit title cannot exceed 200 characters.");
            Title = title;
        }

        if (description != null)
            Description = description;

        if (why != null)
            Why = why;

        if (displayOrder.HasValue)
            DisplayOrder = displayOrder.Value;

        if (roleIds != null)
            _roleIds = roleIds.ToList();

        if (valueIds != null)
            _valueIds = valueIds.ToList();

        if (goalIds != null)
            _goalIds = goalIds.ToList();

        AddDomainEvent(new HabitUpdatedEvent(Id, "Details"));
    }

    public void UpdateSchedule(HabitSchedule schedule)
    {
        if (Status == HabitStatus.Archived)
            throw new DomainException("Cannot update an archived habit.");

        Schedule = schedule ?? throw new DomainException("Schedule is required.");
        AddDomainEvent(new HabitUpdatedEvent(Id, "Schedule"));
    }

    public void UpdatePolicy(HabitPolicy policy)
    {
        if (Status == HabitStatus.Archived)
            throw new DomainException("Cannot update an archived habit.");

        Policy = policy ?? throw new DomainException("Policy is required.");
        AddDomainEvent(new HabitUpdatedEvent(Id, "Policy"));
    }

    public void SetDefaultMode(HabitMode mode)
    {
        if (Status == HabitStatus.Archived)
            throw new DomainException("Cannot update an archived habit.");

        DefaultMode = mode;
        AddDomainEvent(new HabitUpdatedEvent(Id, "DefaultMode"));
    }

    #endregion

    #region Status Transitions

    public void Activate()
    {
        if (Status != HabitStatus.Paused)
            throw new DomainException($"Cannot activate a habit with status {Status}.");

        Status = HabitStatus.Active;
        AddDomainEvent(new HabitStatusChangedEvent(Id, UserId, Status));
    }

    public void Pause()
    {
        if (Status != HabitStatus.Active)
            throw new DomainException("Only active habits can be paused.");

        Status = HabitStatus.Paused;
        AddDomainEvent(new HabitStatusChangedEvent(Id, UserId, Status));
    }

    public void Archive()
    {
        if (Status == HabitStatus.Archived)
            throw new DomainException("Habit is already archived.");

        Status = HabitStatus.Archived;
        AddDomainEvent(new HabitArchivedEvent(Id, UserId));
    }

    #endregion

    #region Metric Bindings

    public HabitMetricBinding AddMetricBinding(
        Guid metricDefinitionId,
        HabitContributionType contributionType,
        decimal? fixedValue = null,
        string? notes = null)
    {
        if (metricDefinitionId == Guid.Empty)
            throw new DomainException("MetricDefinitionId cannot be empty.");

        if (_metricBindings.Any(b => b.MetricDefinitionId == metricDefinitionId))
            throw new DomainException("This metric is already bound to this habit.");

        if (contributionType == HabitContributionType.FixedValue && !fixedValue.HasValue)
            throw new DomainException("FixedValue is required when contribution type is FixedValue.");

        var binding = HabitMetricBinding.Create(
            Id,
            metricDefinitionId,
            contributionType,
            fixedValue,
            notes);

        _metricBindings.Add(binding);
        AddDomainEvent(new HabitUpdatedEvent(Id, "MetricBinding"));

        return binding;
    }

    public void UpdateMetricBinding(
        Guid bindingId,
        HabitContributionType contributionType,
        decimal? fixedValue = null,
        string? notes = null)
    {
        var binding = _metricBindings.FirstOrDefault(b => b.Id == bindingId)
            ?? throw new DomainException("Metric binding not found.");

        binding.Update(contributionType, fixedValue, notes);
        AddDomainEvent(new HabitUpdatedEvent(Id, "MetricBinding"));
    }

    public void RemoveMetricBinding(Guid bindingId)
    {
        var binding = _metricBindings.FirstOrDefault(b => b.Id == bindingId)
            ?? throw new DomainException("Metric binding not found.");

        _metricBindings.Remove(binding);
        AddDomainEvent(new HabitUpdatedEvent(Id, "MetricBinding"));
    }

    #endregion

    #region Variants

    public HabitVariant AddVariant(
        HabitMode mode,
        string label,
        decimal defaultValue,
        int estimatedMinutes,
        int energyCost,
        bool countsAsCompletion = true)
    {
        if (_variants.Any(v => v.Mode == mode))
            throw new DomainException($"A variant for mode {mode} already exists.");

        if (energyCost < 1 || energyCost > 5)
            throw new DomainException("Energy cost must be between 1 and 5.");

        var variant = HabitVariant.Create(
            Id,
            mode,
            label,
            defaultValue,
            estimatedMinutes,
            energyCost,
            countsAsCompletion);

        _variants.Add(variant);
        AddDomainEvent(new HabitUpdatedEvent(Id, "Variant"));

        return variant;
    }

    public void UpdateVariant(
        Guid variantId,
        string? label = null,
        decimal? defaultValue = null,
        int? estimatedMinutes = null,
        int? energyCost = null,
        bool? countsAsCompletion = null)
    {
        var variant = _variants.FirstOrDefault(v => v.Id == variantId)
            ?? throw new DomainException("Variant not found.");

        variant.Update(label, defaultValue, estimatedMinutes, energyCost, countsAsCompletion);
        AddDomainEvent(new HabitUpdatedEvent(Id, "Variant"));
    }

    public void RemoveVariant(Guid variantId)
    {
        var variant = _variants.FirstOrDefault(v => v.Id == variantId)
            ?? throw new DomainException("Variant not found.");

        _variants.Remove(variant);
        AddDomainEvent(new HabitUpdatedEvent(Id, "Variant"));
    }

    public HabitVariant? GetVariant(HabitMode mode) =>
        _variants.FirstOrDefault(v => v.Mode == mode);

    #endregion

    #region Occurrences

    public HabitOccurrence GetOrCreateOccurrence(DateOnly date)
    {
        var existing = _occurrences.FirstOrDefault(o => o.ScheduledOn == date);
        if (existing != null)
            return existing;

        var occurrence = HabitOccurrence.Create(Id, date);
        _occurrences.Add(occurrence);
        return occurrence;
    }

    public HabitOccurrence? GetOccurrence(DateOnly date) =>
        _occurrences.FirstOrDefault(o => o.ScheduledOn == date);

    public IReadOnlyList<HabitOccurrence> GetOccurrencesInRange(DateOnly start, DateOnly end) =>
        _occurrences
            .Where(o => o.ScheduledOn >= start && o.ScheduledOn <= end)
            .OrderBy(o => o.ScheduledOn)
            .ToList();

    public void CompleteOccurrence(
        DateOnly date,
        decimal? enteredValue = null,
        HabitMode? mode = null,
        string? note = null)
    {
        var occurrence = GetOrCreateOccurrence(date);
        var modeUsed = mode ?? DefaultMode;

        occurrence.Complete(enteredValue, modeUsed, note);

        AddDomainEvent(new HabitCompletedEvent(
            occurrence.Id,
            Id,
            UserId,
            date,
            modeUsed,
            enteredValue));
    }

    public void UndoOccurrence(DateOnly date)
    {
        var occurrence = GetOccurrence(date)
            ?? throw new DomainException($"No occurrence found for {date}.");

        if (occurrence.Status != HabitOccurrenceStatus.Completed)
            throw new DomainException("Only completed occurrences can be undone.");

        occurrence.Undo();

        AddDomainEvent(new HabitUndoneEvent(
            occurrence.Id,
            Id,
            date));
    }

    public void SkipOccurrence(DateOnly date, string? reason = null)
    {
        var occurrence = GetOrCreateOccurrence(date);
        occurrence.Skip(reason);

        AddDomainEvent(new HabitSkippedEvent(
            occurrence.Id,
            Id,
            date,
            reason));
    }

    public void MarkOccurrenceMissed(DateOnly date, MissReason reason, string? details = null)
    {
        var occurrence = GetOrCreateOccurrence(date);

        if (Policy.RequireMissReason && reason == MissReason.Other && string.IsNullOrWhiteSpace(details))
            throw new DomainException("Details are required when marking as missed with 'Other' reason.");

        occurrence.MarkMissed(reason, details);

        AddDomainEvent(new HabitMissedEvent(
            occurrence.Id,
            Id,
            date,
            reason));
    }

    public void RescheduleOccurrence(DateOnly date, DateOnly newDate)
    {
        var occurrence = GetOrCreateOccurrence(date);
        occurrence.Reschedule(newDate);

        AddDomainEvent(new HabitOccurrenceRescheduledEvent(
            occurrence.Id, Id, UserId, date, newDate));
    }

    #endregion

    #region Computed Stats

    public void UpdateStreak(int streak)
    {
        var previousStreak = CurrentStreak;
        CurrentStreak = streak;

        // Check for milestone
        if (streak > previousStreak && IsStreakMilestone(streak))
        {
            AddDomainEvent(new HabitStreakMilestoneEvent(
                Id,
                UserId,
                streak,
                GetMilestoneType(streak)));
        }
    }

    public void UpdateAdherenceRate(decimal rate)
    {
        AdherenceRate7Day = Math.Clamp(rate, 0, 100);
    }

    private static bool IsStreakMilestone(int streak) =>
        streak is 7 or 14 or 21 or 30 or 50 or 100 or 365;

    private static string GetMilestoneType(int streak) => streak switch
    {
        7 => "1-week",
        14 => "2-week",
        21 => "3-week",
        30 => "1-month",
        50 => "50-day",
        100 => "100-day",
        365 => "1-year",
        _ => $"{streak}-day"
    };

    #endregion

    #region Query Helpers

    public bool IsDueOn(DateOnly date) =>
        Status == HabitStatus.Active && Schedule.IsDueOn(date);

    public bool HasMetricBindings => _metricBindings.Count > 0;

    public bool HasVariants => _variants.Count > 0;

    public bool RequiresValueEntry =>
        _metricBindings.Any(b => b.ContributionType == HabitContributionType.UseEnteredValue);

    #endregion
}

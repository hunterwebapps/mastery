using Mastery.Domain.Common;
using Mastery.Domain.Events;
using Mastery.Domain.Exceptions;

namespace Mastery.Domain.Entities;

/// <summary>
/// Represents a time-bounded priority context for a user.
/// Seasons are temporary weighting + constraint overlays that tell the system
/// what tradeoffs are acceptable, which roles/goals deserve priority,
/// and what intensity level is realistic.
/// </summary>
public sealed class Season : AuditableEntity, IAggregateRoot
{
    /// <summary>
    /// The user who owns this season.
    /// </summary>
    public string UserId { get; private set; } = null!;

    /// <summary>
    /// User-facing name for the season (e.g., "Q1 Career Ramp", "New Baby Season").
    /// </summary>
    public string Label { get; private set; } = null!;

    /// <summary>
    /// The type of season, which determines default behaviors.
    /// </summary>
    public SeasonType Type { get; private set; }

    /// <summary>
    /// When this season started.
    /// </summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>
    /// When the user expects this season to end (optional).
    /// </summary>
    public DateOnly? ExpectedEndDate { get; private set; }

    /// <summary>
    /// When this season actually ended (set when ending the season).
    /// </summary>
    public DateOnly? ActualEndDate { get; private set; }

    /// <summary>
    /// Role IDs that are the focus of this season.
    /// </summary>
    private List<Guid> _focusRoleIds = [];
    public IReadOnlyList<Guid> FocusRoleIds => _focusRoleIds.AsReadOnly();

    /// <summary>
    /// Goal IDs that are the focus of this season.
    /// </summary>
    private List<Guid> _focusGoalIds = [];
    public IReadOnlyList<Guid> FocusGoalIds => _focusGoalIds.AsReadOnly();

    /// <summary>
    /// One sentence definition of "a good season".
    /// </summary>
    public string? SuccessStatement { get; private set; }

    /// <summary>
    /// Short constraints like "sleep by 11", "Sundays off".
    /// </summary>
    private List<string> _nonNegotiables = [];
    public IReadOnlyList<string> NonNegotiables => _nonNegotiables.AsReadOnly();

    /// <summary>
    /// Intensity level (1-10). Higher = more aggressive planning.
    /// </summary>
    public int Intensity { get; private set; } = 3;

    /// <summary>
    /// Retrospective notes when the season ends.
    /// </summary>
    public string? Outcome { get; private set; }

    /// <summary>
    /// Whether this season has ended.
    /// </summary>
    public bool IsEnded => ActualEndDate.HasValue;

    private Season() { } // EF Core

    public static Season Create(
        string userId,
        string label,
        SeasonType type,
        DateOnly startDate,
        DateOnly? expectedEndDate = null,
        IEnumerable<Guid>? focusRoleIds = null,
        IEnumerable<Guid>? focusGoalIds = null,
        string? successStatement = null,
        IEnumerable<string>? nonNegotiables = null,
        int intensity = 3)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("UserId cannot be empty.");

        if (string.IsNullOrWhiteSpace(label))
            throw new DomainException("Season label cannot be empty.");

        if (intensity < 1 || intensity > 10)
            throw new DomainException("Intensity must be between 1 and 10.");

        if (expectedEndDate.HasValue && expectedEndDate.Value <= startDate)
            throw new DomainException("Expected end date must be after start date.");

        var season = new Season
        {
            UserId = userId,
            Label = label,
            Type = type,
            StartDate = startDate,
            ExpectedEndDate = expectedEndDate,
            SuccessStatement = successStatement,
            Intensity = intensity,
            _focusRoleIds = focusRoleIds?.ToList() ?? [],
            _focusGoalIds = focusGoalIds?.ToList() ?? [],
            _nonNegotiables = nonNegotiables?.ToList() ?? []
        };

        season.AddDomainEvent(new SeasonCreatedEvent(season.Id, userId, type, startDate));

        return season;
    }

    public void Update(
        string? label = null,
        SeasonType? type = null,
        DateOnly? expectedEndDate = null,
        IEnumerable<Guid>? focusRoleIds = null,
        IEnumerable<Guid>? focusGoalIds = null,
        string? successStatement = null,
        IEnumerable<string>? nonNegotiables = null,
        int? intensity = null)
    {
        if (IsEnded)
            throw new DomainException("Cannot update an ended season.");

        if (label != null)
            Label = label;

        if (type.HasValue)
            Type = type.Value;

        if (expectedEndDate.HasValue)
        {
            if (expectedEndDate.Value <= StartDate)
                throw new DomainException("Expected end date must be after start date.");
            ExpectedEndDate = expectedEndDate;
        }

        if (focusRoleIds != null)
            _focusRoleIds = focusRoleIds.ToList();

        if (focusGoalIds != null)
            _focusGoalIds = focusGoalIds.ToList();

        if (successStatement != null)
            SuccessStatement = successStatement;

        if (nonNegotiables != null)
            _nonNegotiables = nonNegotiables.ToList();

        if (intensity.HasValue)
        {
            if (intensity.Value < 1 || intensity.Value > 10)
                throw new DomainException("Intensity must be between 1 and 10.");
            Intensity = intensity.Value;
        }
    }

    public void End(DateOnly endDate, string? outcome = null)
    {
        if (IsEnded)
            throw new DomainException("Season has already ended.");

        if (endDate < StartDate)
            throw new DomainException("End date cannot be before start date.");

        ActualEndDate = endDate;
        Outcome = outcome;

        AddDomainEvent(new SeasonEndedEvent(Id, UserId, endDate, outcome));
    }

    /// <summary>
    /// Gets the capacity utilization target based on season type.
    /// Sprint: 85-95%, Build: 75-85%, Maintain: 65-75%, Recover: 50-65%
    /// </summary>
    public (int MinPercent, int MaxPercent) GetCapacityTargetRange() => Type switch
    {
        SeasonType.Sprint => (85, 95),
        SeasonType.Build => (75, 85),
        SeasonType.Maintain => (65, 75),
        SeasonType.Recover => (50, 65),
        SeasonType.Transition => (60, 70),
        SeasonType.Explore => (55, 70),
        _ => (65, 75)
    };
}

/// <summary>
/// Types of seasons that affect planning engine behavior.
/// </summary>
public enum SeasonType
{
    /// <summary>
    /// High-intensity, goal-focused period. Accept higher load but enforce recovery.
    /// </summary>
    Sprint,

    /// <summary>
    /// Steady progress period. Building habits and systems.
    /// </summary>
    Build,

    /// <summary>
    /// Maintenance mode. Protect capacity and consistency.
    /// </summary>
    Maintain,

    /// <summary>
    /// Recovery period. Minimum versions, emphasize rest.
    /// </summary>
    Recover,

    /// <summary>
    /// Life transition period. Flexible, adaptive planning.
    /// </summary>
    Transition,

    /// <summary>
    /// Exploration period. Trying new things, less rigid structure.
    /// </summary>
    Explore
}

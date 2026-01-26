using Mastery.Domain.Common;
using Mastery.Domain.Events;
using Mastery.Domain.Exceptions;
using Mastery.Domain.ValueObjects;

namespace Mastery.Domain.Entities.UserProfile;

/// <summary>
/// The stable "setpoint + guardrails" aggregate root for a user.
/// Contains identity primitives, preferences, and constraints that the control loop
/// uses to interpret signals and rank actions.
/// Changes infrequently (onboarding, occasional edits, quarterly season reset).
/// </summary>
public sealed class UserProfile : AuditableEntity, IAggregateRoot
{
    /// <summary>
    /// External auth system user ID.
    /// </summary>
    public string UserId { get; private set; } = null!;

    /// <summary>
    /// User's timezone (essential for schedules/check-ins).
    /// </summary>
    public Timezone Timezone { get; private set; } = null!;

    /// <summary>
    /// User's locale for formatting.
    /// </summary>
    public Locale Locale { get; private set; } = null!;

    /// <summary>
    /// Version of onboarding completed (supports progressive profiling).
    /// </summary>
    public int OnboardingVersion { get; private set; }

    /// <summary>
    /// User's core values (stored as JSON).
    /// Soft guideline: 5-10 values.
    /// </summary>
    private List<UserValue> _values = [];
    public IReadOnlyList<UserValue> Values => _values.AsReadOnly();

    /// <summary>
    /// User's life roles (stored as JSON).
    /// Soft guideline: 3-8 active roles.
    /// </summary>
    private List<UserRole> _roles = [];
    public IReadOnlyList<UserRole> Roles => _roles.AsReadOnly();

    /// <summary>
    /// Current season FK (nullable).
    /// </summary>
    public Guid? CurrentSeasonId { get; private set; }

    /// <summary>
    /// Navigation property to current season.
    /// </summary>
    public Season? CurrentSeason { get; private set; }

    /// <summary>
    /// Preferences for how the system interacts with the user.
    /// </summary>
    public Preferences Preferences { get; private set; } = new();

    /// <summary>
    /// Hard constraints the planning engine must respect.
    /// </summary>
    public Constraints Constraints { get; private set; } = new();

    private UserProfile() { } // EF Core

    public static UserProfile Create(
        string userId,
        string timezone,
        string locale,
        int onboardingVersion = 1)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("UserId cannot be empty.");

        var profile = new UserProfile
        {
            UserId = userId,
            Timezone = Timezone.Create(timezone),
            Locale = Locale.Create(locale),
            OnboardingVersion = onboardingVersion
        };

        profile.AddDomainEvent(new UserProfileCreatedEvent(profile.Id, userId));
        return profile;
    }

    public void UpdateValues(IEnumerable<UserValue> values)
    {
        var valuesList = values.ToList();

        // Soft validation - log warning but don't reject
        // Values count outside 5-10 is allowed but not ideal
        _values = valuesList;
        AddDomainEvent(new UserProfileUpdatedEvent(Id, nameof(Values)));
    }

    public void UpdateRoles(IEnumerable<UserRole> roles)
    {
        var rolesList = roles.ToList();

        // Soft validation - log warning but don't reject
        // Active roles count outside 3-8 is allowed but not ideal
        _roles = rolesList;
        AddDomainEvent(new UserProfileUpdatedEvent(Id, nameof(Roles)));
    }

    public void SetCurrentSeason(Season season)
    {
        if (season.UserId != UserId)
            throw new DomainException("Season does not belong to this user.");

        var previousSeasonId = CurrentSeasonId;
        CurrentSeasonId = season.Id;
        CurrentSeason = season;

        AddDomainEvent(new SeasonActivatedEvent(season.Id, UserId, previousSeasonId));
    }

    public void ClearCurrentSeason()
    {
        CurrentSeasonId = null;
        CurrentSeason = null;
    }

    public void UpdatePreferences(Preferences preferences)
    {
        Preferences = preferences ?? throw new DomainException("Preferences cannot be null.");
        AddDomainEvent(new PreferencesUpdatedEvent(Id));
    }

    public void UpdateConstraints(Constraints constraints)
    {
        Constraints = constraints ?? throw new DomainException("Constraints cannot be null.");
        AddDomainEvent(new ConstraintsUpdatedEvent(Id));
    }

    public void UpdateTimezone(string timezone)
    {
        Timezone = Timezone.Create(timezone);
        AddDomainEvent(new UserProfileUpdatedEvent(Id, nameof(Timezone)));
    }

    public void UpdateLocale(string locale)
    {
        Locale = Locale.Create(locale);
        AddDomainEvent(new UserProfileUpdatedEvent(Id, nameof(Locale)));
    }

    public void UpdateOnboardingVersion(int version)
    {
        if (version < OnboardingVersion)
            throw new DomainException("Cannot downgrade onboarding version.");

        OnboardingVersion = version;
    }

    // Query helpers for engines

    /// <summary>
    /// Gets only active roles.
    /// </summary>
    public IEnumerable<UserRole> GetActiveRoles() =>
        _roles.Where(r => r.Status == RoleStatus.Active);

    /// <summary>
    /// Gets active roles ordered by season priority (highest first).
    /// </summary>
    public IEnumerable<UserRole> GetActiveRolesBySeasonPriority() =>
        GetActiveRoles().OrderByDescending(r => r.SeasonPriority).ThenBy(r => r.Rank);

    /// <summary>
    /// Gets values ordered by rank.
    /// </summary>
    public IEnumerable<UserValue> GetValuesOrdered() =>
        _values.OrderBy(v => v.Rank);

    /// <summary>
    /// Gets weekday capacity limit.
    /// </summary>
    public int GetWeekdayCapacityMinutes() => Constraints.MaxPlannedMinutesWeekday;

    /// <summary>
    /// Gets weekend capacity limit.
    /// </summary>
    public int GetWeekendCapacityMinutes() => Constraints.MaxPlannedMinutesWeekend;

    /// <summary>
    /// Gets capacity for a specific day.
    /// </summary>
    public int GetCapacityMinutesForDay(DayOfWeek day) =>
        day is DayOfWeek.Saturday or DayOfWeek.Sunday
            ? GetWeekendCapacityMinutes()
            : GetWeekdayCapacityMinutes();

    /// <summary>
    /// Checks if the user is in focus/sprint mode.
    /// </summary>
    public bool IsInSprintMode() => CurrentSeason?.Type == SeasonType.Sprint;

    /// <summary>
    /// Checks if the user is in recovery mode.
    /// </summary>
    public bool IsInRecoveryMode() => CurrentSeason?.Type == SeasonType.Recover;
}

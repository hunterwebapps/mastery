namespace Mastery.Application.Features.UserProfiles.Models;

/// <summary>
/// Full user profile view model.
/// </summary>
public sealed record UserProfileDto
{
    public Guid Id { get; init; }
    public required string UserId { get; init; }
    public required string Timezone { get; init; }
    public required string Locale { get; init; }
    public int OnboardingVersion { get; init; }
    public IReadOnlyList<UserValueDto> Values { get; init; } = [];
    public IReadOnlyList<UserRoleDto> Roles { get; init; } = [];
    public SeasonDto? CurrentSeason { get; init; }
    public required PreferencesDto Preferences { get; init; }
    public required ConstraintsDto Constraints { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
}

/// <summary>
/// User value DTO.
/// </summary>
public sealed record UserValueDto
{
    public Guid Id { get; init; }
    public string? Key { get; init; }
    public required string Label { get; init; }
    public int Rank { get; init; }
    public decimal? Weight { get; init; }
    public string? Notes { get; init; }
    public string? Source { get; init; }
}

/// <summary>
/// User role DTO.
/// </summary>
public sealed record UserRoleDto
{
    public Guid Id { get; init; }
    public string? Key { get; init; }
    public required string Label { get; init; }
    public int Rank { get; init; }
    public int SeasonPriority { get; init; }
    public int MinWeeklyMinutes { get; init; }
    public int TargetWeeklyMinutes { get; init; }
    public List<string> Tags { get; init; } = [];
    public required string Status { get; init; }
}

/// <summary>
/// Preferences DTO.
/// </summary>
public sealed record PreferencesDto
{
    public required string CoachingStyle { get; init; }
    public required string ExplanationVerbosity { get; init; }
    public required string NudgeLevel { get; init; }
    public List<string> NotificationChannels { get; init; } = [];
    public TimeOnly MorningCheckInTime { get; init; }
    public TimeOnly EveningCheckInTime { get; init; }
    public required PlanningDefaultsDto PlanningDefaults { get; init; }
    public required PrivacySettingsDto Privacy { get; init; }
}

/// <summary>
/// Planning defaults DTO.
/// </summary>
public sealed record PlanningDefaultsDto
{
    public int DefaultTaskDurationMinutes { get; init; }
    public bool AutoScheduleHabits { get; init; }
    public int BufferBetweenTasksMinutes { get; init; }
}

/// <summary>
/// Privacy settings DTO.
/// </summary>
public sealed record PrivacySettingsDto
{
    public bool ShareProgressWithCoach { get; init; }
    public bool AllowAnonymousAnalytics { get; init; }
}

/// <summary>
/// Constraints DTO.
/// </summary>
public sealed record ConstraintsDto
{
    public int MaxPlannedMinutesWeekday { get; init; }
    public int MaxPlannedMinutesWeekend { get; init; }
    public List<BlockedWindowDto> BlockedTimeWindows { get; init; } = [];
    public List<TimeWindowDto> NoNotificationsWindows { get; init; } = [];
    public string? HealthNotes { get; init; }
    public List<string> ContentBoundaries { get; init; } = [];
}

/// <summary>
/// Blocked time window DTO.
/// </summary>
public sealed record BlockedWindowDto
{
    public string? Label { get; init; }
    public required TimeWindowDto TimeWindow { get; init; }
    public List<DayOfWeek> ApplicableDays { get; init; } = [];
}

/// <summary>
/// Time window DTO.
/// </summary>
public sealed record TimeWindowDto
{
    public TimeOnly Start { get; init; }
    public TimeOnly End { get; init; }
}

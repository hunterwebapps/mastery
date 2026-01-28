namespace Mastery.Application.Features.Habits.Models;

/// <summary>
/// Full habit DTO with all details including bindings and variants.
/// </summary>
public sealed record HabitDto
{
    public Guid Id { get; init; }
    public required string UserId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public string? Why { get; init; }
    public required string Status { get; init; }
    public int DisplayOrder { get; init; }
    public required HabitScheduleDto Schedule { get; init; }
    public required HabitPolicyDto Policy { get; init; }
    public required string DefaultMode { get; init; }
    public List<Guid> RoleIds { get; init; } = [];
    public List<Guid> ValueIds { get; init; } = [];
    public List<Guid> GoalIds { get; init; } = [];
    public List<HabitMetricBindingDto> MetricBindings { get; init; } = [];
    public List<HabitVariantDto> Variants { get; init; } = [];
    public int CurrentStreak { get; init; }
    public decimal AdherenceRate7Day { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
}

/// <summary>
/// Habit summary for list views - lightweight without nested details.
/// </summary>
public sealed record HabitSummaryDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string Status { get; init; }
    public required string DefaultMode { get; init; }
    public int DisplayOrder { get; init; }
    public required string ScheduleType { get; init; }
    public string ScheduleDescription { get; init; } = string.Empty;
    public int MetricBindingCount { get; init; }
    public int VariantCount { get; init; }
    public int CurrentStreak { get; init; }
    public decimal AdherenceRate7Day { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Schedule configuration for a habit.
/// </summary>
public sealed record HabitScheduleDto
{
    public required string Type { get; init; }
    public List<int>? DaysOfWeek { get; init; }
    public List<string>? PreferredTimes { get; init; }
    public int? FrequencyPerWeek { get; init; }
    public int? IntervalDays { get; init; }
    public required string StartDate { get; init; }
    public string? EndDate { get; init; }
}

/// <summary>
/// Policy configuration for a habit.
/// </summary>
public sealed record HabitPolicyDto
{
    public bool AllowLateCompletion { get; init; }
    public string? LateCutoffTime { get; init; }
    public bool AllowSkip { get; init; }
    public bool RequireMissReason { get; init; }
    public bool AllowBackfill { get; init; }
    public int MaxBackfillDays { get; init; }
}

/// <summary>
/// Metric binding for a habit.
/// </summary>
public sealed record HabitMetricBindingDto
{
    public Guid Id { get; init; }
    public Guid MetricDefinitionId { get; init; }
    public string? MetricName { get; init; }
    public required string ContributionType { get; init; }
    public decimal? FixedValue { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Mode variant for a habit.
/// </summary>
public sealed record HabitVariantDto
{
    public Guid Id { get; init; }
    public required string Mode { get; init; }
    public required string Label { get; init; }
    public decimal DefaultValue { get; init; }
    public int EstimatedMinutes { get; init; }
    public int EnergyCost { get; init; }
    public bool CountsAsCompletion { get; init; }
}

/// <summary>
/// Habit occurrence (scheduled instance).
/// </summary>
public sealed record HabitOccurrenceDto
{
    public Guid Id { get; init; }
    public Guid HabitId { get; init; }
    public required string ScheduledOn { get; init; }
    public required string Status { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? CompletedOn { get; init; }
    public string? ModeUsed { get; init; }
    public decimal? EnteredValue { get; init; }
    public string? MissReason { get; init; }
    public string? Note { get; init; }
    public string? RescheduledTo { get; init; }
}

/// <summary>
/// Optimized projection for Today view - critical for daily loop.
/// </summary>
public sealed record TodayHabitDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public bool IsDue { get; init; }
    public required string DefaultMode { get; init; }
    public HabitOccurrenceDto? TodayOccurrence { get; init; }
    public List<HabitVariantDto> Variants { get; init; } = [];
    public int CurrentStreak { get; init; }
    public decimal AdherenceRate7Day { get; init; }
    public List<string> GoalImpactTags { get; init; } = [];
    public bool RequiresValueEntry { get; init; }
    public int DisplayOrder { get; init; }
}

/// <summary>
/// Habit statistics and analytics.
/// </summary>
public sealed record HabitStatsDto
{
    public Guid HabitId { get; init; }
    public int CurrentStreak { get; init; }
    public int LongestStreak { get; init; }
    public decimal AdherenceRate7Day { get; init; }
    public decimal AdherenceRate30Day { get; init; }
    public int TotalCompletions { get; init; }
    public int TotalMissed { get; init; }
    public int TotalSkipped { get; init; }
    public Dictionary<string, int> CompletionsByDayOfWeek { get; init; } = new();
    public Dictionary<string, int> MissReasonDistribution { get; init; } = new();
    public List<StreakPeriodDto> RecentStreaks { get; init; } = [];
}

/// <summary>
/// A streak period for history tracking.
/// </summary>
public sealed record StreakPeriodDto
{
    public required string StartDate { get; init; }
    public required string EndDate { get; init; }
    public int Length { get; init; }
}

/// <summary>
/// Habit history with occurrences in a date range.
/// </summary>
public sealed record HabitHistoryDto
{
    public Guid HabitId { get; init; }
    public required string FromDate { get; init; }
    public required string ToDate { get; init; }
    public List<HabitOccurrenceDto> Occurrences { get; init; } = [];
    public int TotalDue { get; init; }
    public int TotalCompleted { get; init; }
    public int TotalMissed { get; init; }
    public int TotalSkipped { get; init; }
}

/// <summary>
/// Upcoming habits schedule.
/// </summary>
public sealed record UpcomingHabitDto
{
    public Guid HabitId { get; init; }
    public required string Title { get; init; }
    public required string ScheduledOn { get; init; }
    public string? ExistingStatus { get; init; }
}

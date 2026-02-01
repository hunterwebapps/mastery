using Mastery.Domain.Enums;
using TaskStatus = Mastery.Domain.Enums.TaskStatus;

namespace Mastery.Domain.Diagnostics.Snapshots;

/// <summary>
/// Read-only snapshot of a user's current state, assembled from existing repositories.
/// Used by signal detectors and candidate generators.
/// </summary>
public sealed record UserStateSnapshot(
    string UserId,
    UserProfileSnapshot Profile,
    IReadOnlyList<GoalSnapshot> Goals,
    IReadOnlyList<HabitSnapshot> Habits,
    IReadOnlyList<TaskSnapshot> Tasks,
    IReadOnlyList<ProjectSnapshot> Projects,
    IReadOnlyList<ExperimentSnapshot> Experiments,
    IReadOnlyList<CheckInSnapshot> RecentCheckIns,
    IReadOnlyList<MetricDefinitionSnapshot> MetricDefinitions,
    int CheckInStreak,
    DateOnly Today);

public sealed record GoalSnapshot(
    Guid Id,
    string Title,
    GoalStatus Status,
    int Priority,
    DateOnly? Deadline,
    IReadOnlyList<GoalMetricSnapshot> Metrics);

public sealed record GoalMetricSnapshot(
    Guid Id,
    Guid MetricDefinitionId,
    string MetricName,
    MetricKind Kind,
    decimal TargetValue,
    decimal? CurrentValue,
    MetricSourceType SourceHint,
    // Extended fields for richer LLM context
    string TargetType,
    decimal? TargetMaxValue,
    string WindowType,
    int? RollingDays,
    string Aggregation,
    decimal Weight,
    decimal? Baseline);

public sealed record HabitSnapshot(
    Guid Id,
    string Title,
    HabitStatus Status,
    HabitMode CurrentMode,
    decimal Adherence7Day,
    int CurrentStreak,
    IReadOnlyList<Guid> MetricBindingIds,
    HabitScheduleSnapshot? Schedule,
    IReadOnlyList<HabitVariantSnapshot>? Variants,
    IReadOnlyList<Guid>? GoalIds);

public sealed record HabitScheduleSnapshot(
    string Type,
    int[]? DaysOfWeek,
    int? FrequencyPerWeek,
    int? IntervalDays);

public sealed record HabitVariantSnapshot(
    string Mode,
    string Label,
    int EstimatedMinutes,
    int EnergyCost);

public sealed record TaskSnapshot(
    Guid Id,
    string Title,
    TaskStatus Status,
    int? EstMinutes,
    int EnergyLevel,
    int Priority,
    Guid? ProjectId,
    Guid? GoalId,
    DateOnly? ScheduledDate,
    DateOnly? DueDate,
    DueType? DueType,
    int RescheduleCount,
    IReadOnlyList<string> ContextTags);

public sealed record ProjectSnapshot(
    Guid Id,
    string Title,
    ProjectStatus Status,
    Guid? GoalId,
    Guid? NextTaskId,
    int TotalTasks,
    int CompletedTasks,
    DateOnly? TargetEndDate,
    int Priority,
    IReadOnlyList<MilestoneSnapshot>? Milestones);

public sealed record MilestoneSnapshot(
    Guid Id,
    string Title,
    string Status,
    DateOnly? TargetDate);

public sealed record ExperimentSnapshot(
    Guid Id,
    string Title,
    ExperimentStatus Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    // Extended fields for richer LLM context
    string Category,
    IReadOnlyList<Guid> LinkedGoalIds,
    ExperimentHypothesisSnapshot? Hypothesis,
    ExperimentMeasurementPlanSnapshot? MeasurementPlan);

public sealed record ExperimentHypothesisSnapshot(
    string Change,
    string ExpectedOutcome,
    string? Rationale);

public sealed record ExperimentMeasurementPlanSnapshot(
    Guid PrimaryMetricDefinitionId,
    string PrimaryAggregation,
    int BaselineWindowDays,
    int RunWindowDays,
    IReadOnlyList<Guid> GuardrailMetricDefinitionIds);

public sealed record CheckInSnapshot(
    Guid Id,
    DateOnly Date,
    CheckInType Type,
    CheckInStatus Status,
    int? EnergyLevel,
    string? Top1Type,
    Guid? Top1EntityId,
    bool? Top1Completed);

public sealed record MetricDefinitionSnapshot(
    Guid Id,
    string Name,
    string? Description,
    string DataType,
    string Direction,
    MetricSourceType SourceType,
    DateTime? LastObservationDate,
    // Extended fields for richer LLM context
    string? UnitType,
    string? UnitDisplayLabel,
    string DefaultCadence,
    string DefaultAggregation,
    IReadOnlyList<string> Tags);

// ─────────────────────────────────────────────────────────────────
// User Profile Snapshots
// ─────────────────────────────────────────────────────────────────

public sealed record UserProfileSnapshot(
    string Timezone,
    string Locale,
    IReadOnlyList<UserValueSnapshot> Values,
    IReadOnlyList<UserRoleSnapshot> Roles,
    SeasonSnapshot? CurrentSeason,
    PreferencesSnapshot Preferences,
    ConstraintsSnapshot Constraints);

public sealed record UserValueSnapshot(
    string Label,
    string? Key,
    int Rank);

public sealed record UserRoleSnapshot(
    Guid Id,
    string Label,
    string? Key,
    int Rank,
    int SeasonPriority,
    int MinWeeklyMinutes,
    int TargetWeeklyMinutes,
    IReadOnlyList<string> Tags,
    bool IsActive);

public sealed record SeasonSnapshot(
    Guid Id,
    string Label,
    string Type,
    int Intensity,
    string? SuccessStatement,
    IReadOnlyList<string> NonNegotiables,
    IReadOnlyList<Guid> FocusRoleIds,
    IReadOnlyList<Guid> FocusGoalIds,
    DateOnly StartDate,
    DateOnly? ExpectedEndDate);

public sealed record PreferencesSnapshot(
    string CoachingStyle,
    string Verbosity,
    string NudgeLevel);

public sealed record ConstraintsSnapshot(
    int MaxPlannedMinutesWeekday,
    int MaxPlannedMinutesWeekend,
    string? HealthNotes,
    IReadOnlyList<string> ContentBoundaries);

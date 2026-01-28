using Mastery.Domain.Enums;
using TaskStatus = Mastery.Domain.Enums.TaskStatus;

namespace Mastery.Application.Common.Models;

/// <summary>
/// Read-only snapshot of a user's current state, assembled from existing repositories.
/// Used by signal detectors and candidate generators.
/// </summary>
public sealed record UserStateSnapshot(
    string UserId,
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
    MetricSourceType SourceHint);

public sealed record HabitSnapshot(
    Guid Id,
    string Title,
    HabitStatus Status,
    HabitMode CurrentMode,
    decimal Adherence7Day,
    int CurrentStreak,
    IReadOnlyList<Guid> MetricBindingIds);

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
    DateOnly? TargetEndDate);

public sealed record ExperimentSnapshot(
    Guid Id,
    string Title,
    ExperimentStatus Status,
    DateOnly? StartDate,
    DateOnly? EndDate);

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
    MetricSourceType SourceType,
    DateTime? LastObservationDate);

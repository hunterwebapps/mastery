using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Habits.Commands.CreateHabit;

/// <summary>
/// Creates a new habit for the current user.
/// </summary>
public sealed record CreateHabitCommand(
    string Title,
    CreateHabitScheduleInput Schedule,
    string? Description = null,
    string? Why = null,
    CreateHabitPolicyInput? Policy = null,
    string DefaultMode = "Full",
    List<CreateHabitMetricBindingInput>? MetricBindings = null,
    List<CreateHabitVariantInput>? Variants = null,
    List<Guid>? RoleIds = null,
    List<Guid>? ValueIds = null,
    List<Guid>? GoalIds = null) : ICommand<Guid>;

/// <summary>
/// Input for creating a habit schedule.
/// </summary>
public sealed record CreateHabitScheduleInput(
    string Type,
    List<int>? DaysOfWeek = null,
    List<string>? PreferredTimes = null,
    int? FrequencyPerWeek = null,
    int? IntervalDays = null,
    string? StartDate = null,
    string? EndDate = null);

/// <summary>
/// Input for creating a habit policy.
/// </summary>
public sealed record CreateHabitPolicyInput(
    bool AllowLateCompletion = true,
    string? LateCutoffTime = null,
    bool AllowSkip = true,
    bool RequireMissReason = false,
    bool AllowBackfill = true,
    int MaxBackfillDays = 7);

/// <summary>
/// Input for creating a metric binding.
/// </summary>
public sealed record CreateHabitMetricBindingInput(
    Guid MetricDefinitionId,
    string ContributionType,
    decimal? FixedValue = null,
    string? Notes = null);

/// <summary>
/// Input for creating a mode variant.
/// </summary>
public sealed record CreateHabitVariantInput(
    string Mode,
    string Label,
    decimal DefaultValue,
    int EstimatedMinutes,
    int EnergyCost,
    bool CountsAsCompletion = true);

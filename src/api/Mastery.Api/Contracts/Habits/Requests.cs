namespace Mastery.Api.Contracts.Habits;

/// <summary>
/// Request to create a new habit.
/// </summary>
public sealed record CreateHabitRequest(
    string Title,
    CreateHabitScheduleRequest Schedule,
    string? Description = null,
    string? Why = null,
    CreateHabitPolicyRequest? Policy = null,
    string DefaultMode = "Full",
    List<CreateHabitMetricBindingRequest>? MetricBindings = null,
    List<CreateHabitVariantRequest>? Variants = null,
    List<Guid>? RoleIds = null,
    List<Guid>? ValueIds = null,
    List<Guid>? GoalIds = null);

/// <summary>
/// Request to create a habit schedule.
/// </summary>
public sealed record CreateHabitScheduleRequest(
    string Type,
    List<int>? DaysOfWeek = null,
    List<string>? PreferredTimes = null,
    int? FrequencyPerWeek = null,
    int? IntervalDays = null,
    string? StartDate = null,
    string? EndDate = null);

/// <summary>
/// Request to create a habit policy.
/// </summary>
public sealed record CreateHabitPolicyRequest(
    bool AllowLateCompletion = true,
    string? LateCutoffTime = null,
    bool AllowSkip = true,
    bool RequireMissReason = false,
    bool AllowBackfill = true,
    int MaxBackfillDays = 7);

/// <summary>
/// Request to create a metric binding.
/// </summary>
public sealed record CreateHabitMetricBindingRequest(
    Guid MetricDefinitionId,
    string ContributionType,
    decimal? FixedValue = null,
    string? Notes = null);

/// <summary>
/// Request to create a mode variant.
/// </summary>
public sealed record CreateHabitVariantRequest(
    string Mode,
    string Label,
    decimal DefaultValue,
    int EstimatedMinutes,
    int EnergyCost,
    bool CountsAsCompletion = true);

/// <summary>
/// Request to update a habit.
/// </summary>
public sealed record UpdateHabitRequest(
    string? Title = null,
    string? Description = null,
    string? Why = null,
    string? DefaultMode = null,
    CreateHabitScheduleRequest? Schedule = null,
    CreateHabitPolicyRequest? Policy = null,
    List<Guid>? RoleIds = null,
    List<Guid>? ValueIds = null,
    List<Guid>? GoalIds = null);

/// <summary>
/// Request to update habit status.
/// </summary>
public sealed record UpdateHabitStatusRequest(
    string NewStatus);

/// <summary>
/// Request to complete a habit occurrence.
/// </summary>
public sealed record CompleteOccurrenceRequest(
    string? Mode = null,
    decimal? Value = null,
    string? Note = null);

/// <summary>
/// Request to skip a habit occurrence.
/// </summary>
public sealed record SkipOccurrenceRequest(
    string? Reason = null);

/// <summary>
/// Request to mark a habit occurrence as missed.
/// </summary>
public sealed record MarkMissedRequest(
    string Reason,
    string? Details = null);

/// <summary>
/// Request to batch complete multiple habits.
/// </summary>
public sealed record BatchCompleteRequest(
    List<BatchCompleteItem> Items);

/// <summary>
/// Item in a batch complete request.
/// </summary>
public sealed record BatchCompleteItem(
    Guid HabitId,
    string Date,
    string? Mode = null,
    decimal? Value = null);

/// <summary>
/// Request to reorder habits.
/// </summary>
public sealed record ReorderHabitsRequest(
    List<Guid> HabitIds);

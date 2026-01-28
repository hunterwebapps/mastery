namespace Mastery.Api.Contracts.Goals;

/// <summary>
/// Request to create a new goal.
/// </summary>
public sealed record CreateGoalRequest(
    string Title,
    string? Description = null,
    string? Why = null,
    int Priority = 3,
    DateOnly? Deadline = null,
    Guid? SeasonId = null,
    List<Guid>? RoleIds = null,
    List<Guid>? ValueIds = null,
    List<Guid>? DependencyIds = null,
    List<CreateGoalMetricRequest>? Metrics = null);

/// <summary>
/// Request to create a metric on a goal.
/// </summary>
public sealed record CreateGoalMetricRequest(
    Guid MetricDefinitionId,
    string Kind,
    CreateTargetRequest Target,
    CreateEvaluationWindowRequest EvaluationWindow,
    string Aggregation,
    string SourceHint,
    decimal Weight = 1.0m,
    int DisplayOrder = 0,
    decimal? Baseline = null,
    decimal? MinimumThreshold = null);

/// <summary>
/// Request to create a target.
/// </summary>
public sealed record CreateTargetRequest(
    string Type,
    decimal Value,
    decimal? MaxValue = null);

/// <summary>
/// Request to create an evaluation window.
/// </summary>
public sealed record CreateEvaluationWindowRequest(
    string WindowType,
    int? RollingDays = null,
    int? StartDay = null);

/// <summary>
/// Request to update a goal.
/// </summary>
public sealed record UpdateGoalRequest(
    string Title,
    string? Description = null,
    string? Why = null,
    int Priority = 3,
    DateOnly? Deadline = null,
    Guid? SeasonId = null,
    List<Guid>? RoleIds = null,
    List<Guid>? ValueIds = null,
    List<Guid>? DependencyIds = null);

/// <summary>
/// Request to update goal status.
/// </summary>
public sealed record UpdateGoalStatusRequest(
    string NewStatus,
    string? CompletionNotes = null);

/// <summary>
/// Request to update a goal's scoreboard (metrics).
/// </summary>
public sealed record UpdateGoalScoreboardRequest(
    List<UpdateGoalMetricRequest> Metrics);

/// <summary>
/// Request to update a metric on a goal.
/// </summary>
public sealed record UpdateGoalMetricRequest(
    Guid? Id,
    Guid MetricDefinitionId,
    string Kind,
    CreateTargetRequest Target,
    CreateEvaluationWindowRequest EvaluationWindow,
    string Aggregation,
    string SourceHint,
    decimal Weight = 1.0m,
    int DisplayOrder = 0,
    decimal? Baseline = null,
    decimal? MinimumThreshold = null);

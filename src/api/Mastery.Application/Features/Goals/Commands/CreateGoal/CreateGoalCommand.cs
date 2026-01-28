using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Goals.Commands.CreateGoal;

/// <summary>
/// Creates a new goal for the current user.
/// </summary>
public sealed record CreateGoalCommand(
    string Title,
    string? Description = null,
    string? Why = null,
    int Priority = 3,
    DateOnly? Deadline = null,
    Guid? SeasonId = null,
    List<Guid>? RoleIds = null,
    List<Guid>? ValueIds = null,
    List<Guid>? DependencyIds = null,
    List<CreateGoalMetricInput>? Metrics = null) : ICommand<Guid>;

/// <summary>
/// Input for creating a metric on a goal.
/// </summary>
public sealed record CreateGoalMetricInput(
    Guid MetricDefinitionId,
    string Kind,
    CreateTargetInput Target,
    CreateEvaluationWindowInput EvaluationWindow,
    string Aggregation,
    string SourceHint,
    decimal Weight = 1.0m,
    int DisplayOrder = 0,
    decimal? Baseline = null,
    decimal? MinimumThreshold = null);

/// <summary>
/// Input for creating a target.
/// </summary>
public sealed record CreateTargetInput(
    string Type,
    decimal Value,
    decimal? MaxValue = null);

/// <summary>
/// Input for creating an evaluation window.
/// </summary>
public sealed record CreateEvaluationWindowInput(
    string WindowType,
    int? RollingDays = null,
    int? StartDay = null);

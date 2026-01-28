using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Goals.Commands.CreateGoal;

namespace Mastery.Application.Features.Goals.Commands.UpdateGoalScoreboard;

/// <summary>
/// Replaces all metrics on a goal's scoreboard.
/// </summary>
public sealed record UpdateGoalScoreboardCommand(
    Guid GoalId,
    List<UpdateGoalMetricInput> Metrics) : ICommand;

/// <summary>
/// Input for updating or adding a metric on a goal.
/// </summary>
public sealed record UpdateGoalMetricInput(
    Guid? Id,  // Existing metric ID, null for new
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

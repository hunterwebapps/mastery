using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Goals.Commands.AddGoalMetric;

/// <summary>
/// Adds a single metric to a goal's scoreboard.
/// Can optionally create the metric definition first if not provided.
/// </summary>
public sealed record AddGoalMetricCommand(
    // Goal
    Guid GoalId,

    // MetricDefinition - either existing ID or new definition fields
    Guid? ExistingMetricDefinitionId,
    string? NewMetricName,
    string? NewMetricDescription,
    string? NewMetricDataType,
    string? NewMetricDirection,

    // GoalMetric configuration
    string Kind,
    string TargetType,
    decimal TargetValue,
    decimal? TargetMaxValue,
    string WindowType,
    int? RollingDays,
    int? WeekStartDay,
    string Aggregation,
    string SourceHint,
    decimal Weight = 1.0m,
    decimal? Baseline = null,
    decimal? MinimumThreshold = null) : ICommand<AddGoalMetricResult>;

public sealed record AddGoalMetricResult(
    Guid GoalId,
    Guid MetricDefinitionId,
    Guid GoalMetricId,
    bool MetricDefinitionCreated);

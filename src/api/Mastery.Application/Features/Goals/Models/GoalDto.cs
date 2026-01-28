namespace Mastery.Application.Features.Goals.Models;

/// <summary>
/// Full goal DTO with all details including metrics.
/// </summary>
public sealed record GoalDto
{
    public Guid Id { get; init; }
    public required string UserId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public string? Why { get; init; }
    public required string Status { get; init; }
    public int Priority { get; init; }
    public DateOnly? Deadline { get; init; }
    public Guid? SeasonId { get; init; }
    public List<Guid> RoleIds { get; init; } = [];
    public List<Guid> ValueIds { get; init; } = [];
    public List<Guid> DependencyIds { get; init; } = [];
    public List<GoalMetricDto> Metrics { get; init; } = [];
    public string? CompletionNotes { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
}

/// <summary>
/// Goal summary for list views - lightweight without nested metrics.
/// </summary>
public sealed record GoalSummaryDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Status { get; init; }
    public int Priority { get; init; }
    public DateOnly? Deadline { get; init; }
    public Guid? SeasonId { get; init; }
    public int MetricCount { get; init; }
    public int LagMetricCount { get; init; }
    public int LeadMetricCount { get; init; }
    public int ConstraintMetricCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Goal metric within a goal's scoreboard.
/// </summary>
public sealed record GoalMetricDto
{
    public Guid Id { get; init; }
    public Guid MetricDefinitionId { get; init; }
    public required string MetricName { get; init; }
    public required string Kind { get; init; }
    public required TargetDto Target { get; init; }
    public required EvaluationWindowDto EvaluationWindow { get; init; }
    public required string Aggregation { get; init; }
    public decimal Weight { get; init; }
    public required string SourceHint { get; init; }
    public int DisplayOrder { get; init; }
    public decimal? Baseline { get; init; }
    public decimal? MinimumThreshold { get; init; }
    public MetricUnitDto? Unit { get; init; }
}

/// <summary>
/// Target configuration for a goal metric.
/// </summary>
public sealed record TargetDto
{
    public required string Type { get; init; }
    public decimal Value { get; init; }
    public decimal? MaxValue { get; init; }
}

/// <summary>
/// Evaluation window configuration.
/// </summary>
public sealed record EvaluationWindowDto
{
    public required string WindowType { get; init; }
    public int? RollingDays { get; init; }
    public int? StartDay { get; init; }
}

/// <summary>
/// Metric unit for display.
/// </summary>
public sealed record MetricUnitDto
{
    public required string Type { get; init; }
    public required string Label { get; init; }
}

/// <summary>
/// Goal scoreboard with computed values.
/// </summary>
public sealed record GoalScoreboardDto
{
    public Guid GoalId { get; init; }
    public List<ScoreboardMetricDto> Metrics { get; init; } = [];
    public decimal? OverallScore { get; init; }
    public string? HealthStatus { get; init; }
}

/// <summary>
/// Metric with current computed value.
/// </summary>
public sealed record ScoreboardMetricDto
{
    public Guid GoalMetricId { get; init; }
    public required string MetricName { get; init; }
    public required string Kind { get; init; }
    public decimal? CurrentValue { get; init; }
    public decimal TargetValue { get; init; }
    public decimal? Progress { get; init; }
    public bool IsOnTrack { get; init; }
    public string? TrendDirection { get; init; }
    public MetricUnitDto? Unit { get; init; }
}

namespace Mastery.Application.Features.Metrics.Models;

/// <summary>
/// Metric definition from the user's metric library.
/// </summary>
public sealed record MetricDefinitionDto
{
    public Guid Id { get; init; }
    public required string UserId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string DataType { get; init; }
    public MetricUnitDto? Unit { get; init; }
    public required string Direction { get; init; }
    public required string DefaultCadence { get; init; }
    public required string DefaultAggregation { get; init; }
    public bool IsArchived { get; init; }
    public List<string> Tags { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
}

/// <summary>
/// Summary of a metric definition for quick selection.
/// </summary>
public sealed record MetricDefinitionSummaryDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string DataType { get; init; }
    public required string Direction { get; init; }
    public MetricUnitDto? Unit { get; init; }
}

/// <summary>
/// Metric unit configuration.
/// </summary>
public sealed record MetricUnitDto
{
    public required string Type { get; init; }
    public required string Label { get; init; }
}

/// <summary>
/// A single metric observation (data point).
/// </summary>
public sealed record MetricObservationDto
{
    public Guid Id { get; init; }
    public Guid MetricDefinitionId { get; init; }
    public required string UserId { get; init; }
    public DateTime ObservedAt { get; init; }
    public DateOnly ObservedOn { get; init; }
    public decimal Value { get; init; }
    public required string Source { get; init; }
    public string? CorrelationId { get; init; }
    public string? Note { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsCorrected { get; init; }
}

/// <summary>
/// Time-series of observations for a metric.
/// </summary>
public sealed record MetricTimeSeriesDto
{
    public Guid MetricDefinitionId { get; init; }
    public required string MetricName { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public List<MetricDataPointDto> DataPoints { get; init; } = [];
    public decimal? AggregatedValue { get; init; }
    public int Count { get; init; }
}

/// <summary>
/// A simplified data point for charting.
/// </summary>
public sealed record MetricDataPointDto
{
    public DateOnly Date { get; init; }
    public decimal Value { get; init; }
    public string? Note { get; init; }
}

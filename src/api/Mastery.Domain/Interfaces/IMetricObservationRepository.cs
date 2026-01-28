using Mastery.Domain.Entities.Metrics;

namespace Mastery.Domain.Interfaces;

/// <summary>
/// Repository for metric observations.
/// Note: MetricObservation is not an aggregate root, so this doesn't extend IRepository.
/// </summary>
public interface IMetricObservationRepository
{
    /// <summary>
    /// Gets an observation by ID.
    /// </summary>
    Task<MetricObservation?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new observation.
    /// </summary>
    Task<MetricObservation> AddAsync(
        MetricObservation observation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets observations for a metric within a date range.
    /// </summary>
    Task<IReadOnlyList<MetricObservation>> GetByMetricAndDateRangeAsync(
        Guid metricDefinitionId,
        string userId,
        DateOnly startDate,
        DateOnly endDate,
        bool excludeCorrected = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent observation for a metric.
    /// </summary>
    Task<MetricObservation?> GetLatestByMetricAsync(
        Guid metricDefinitionId,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets observations for multiple metrics within a date range.
    /// </summary>
    Task<IReadOnlyList<MetricObservation>> GetByMetricsAndDateRangeAsync(
        IEnumerable<Guid> metricDefinitionIds,
        string userId,
        DateOnly startDate,
        DateOnly endDate,
        bool excludeCorrected = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all observations for a user on a specific date.
    /// </summary>
    Task<IReadOnlyList<MetricObservation>> GetByUserAndDateAsync(
        string userId,
        DateOnly date,
        bool excludeCorrected = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts observations for a metric within a date range.
    /// </summary>
    Task<int> CountByMetricAndDateRangeAsync(
        Guid metricDefinitionId,
        string userId,
        DateOnly startDate,
        DateOnly endDate,
        bool excludeCorrected = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the aggregated value for a metric within a date range.
    /// </summary>
    Task<decimal?> GetAggregatedValueAsync(
        Guid metricDefinitionId,
        string userId,
        DateOnly startDate,
        DateOnly endDate,
        Domain.Enums.MetricAggregation aggregation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets observations by correlation ID (e.g., HabitOccurrence:guid).
    /// </summary>
    Task<IReadOnlyList<MetricObservation>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default);
}

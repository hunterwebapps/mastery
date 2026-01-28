using Mastery.Domain.Entities.Metrics;

namespace Mastery.Domain.Interfaces;

public interface IMetricDefinitionRepository : IRepository<MetricDefinition>
{
    /// <summary>
    /// Gets all metric definitions for a user (including archived if specified).
    /// </summary>
    Task<IReadOnlyList<MetricDefinition>> GetByUserIdAsync(
        string userId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a metric definition by name for a user.
    /// </summary>
    Task<MetricDefinition?> GetByUserIdAndNameAsync(
        string userId,
        string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a metric definition exists and belongs to the specified user.
    /// </summary>
    Task<bool> ExistsByIdAndUserIdAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a metric with the given name already exists for the user.
    /// </summary>
    Task<bool> ExistsByUserIdAndNameAsync(
        string userId,
        string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple metric definitions by their IDs.
    /// </summary>
    Task<IReadOnlyList<MetricDefinition>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default);
}

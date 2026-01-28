using Mastery.Domain.Entities.Experiment;
using Mastery.Domain.Enums;

namespace Mastery.Domain.Interfaces;

public interface IExperimentRepository : IRepository<Experiment>
{
    /// <summary>
    /// Gets all experiments for a user, ordered by creation date.
    /// </summary>
    Task<IReadOnlyList<Experiment>> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets experiments for a user filtered by status.
    /// </summary>
    Task<IReadOnlyList<Experiment>> GetByUserIdAndStatusAsync(
        string userId,
        ExperimentStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currently active experiment for a user (if any).
    /// Returns null if no experiment is active.
    /// </summary>
    Task<Experiment?> GetActiveByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an experiment with its notes and result eagerly loaded.
    /// </summary>
    Task<Experiment?> GetByIdWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the user has an active (running) experiment.
    /// </summary>
    Task<bool> HasActiveExperimentAsync(
        string userId,
        CancellationToken cancellationToken = default);
}

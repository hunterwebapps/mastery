namespace Mastery.Application.Common.Interfaces;

/// <summary>
/// Resolves entities by type and ID for outbox processing.
/// Used by the embedding processor to fetch current entity state.
/// </summary>
public interface IEntityResolver
{
    /// <summary>
    /// Resolves an entity by type and ID.
    /// Returns null if the entity is not found (e.g., has been deleted).
    /// </summary>
    /// <param name="entityType">The type name of the entity (e.g., "Goal", "Habit").</param>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The entity if found, null otherwise.</returns>
    Task<object?> ResolveAsync(string entityType, Guid entityId, CancellationToken ct);
}

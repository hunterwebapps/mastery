namespace Mastery.Application.Common.Interfaces;

/// <summary>
/// Factory for resolving embedding text strategies by entity type.
/// </summary>
public interface IEmbeddingTextStrategyFactory
{
    /// <summary>
    /// Compiles an entity into text for embedding generation.
    /// </summary>
    /// <param name="entityType">The entity type name (e.g., "Goal", "Habit").</param>
    /// <param name="entity">The entity instance.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The compiled text, or null if the entity should not be embedded.</returns>
    Task<string?> CompileTextAsync(string entityType, object entity, CancellationToken ct);

    /// <summary>
    /// Gets the user ID from an entity.
    /// </summary>
    /// <param name="entity">The entity instance.</param>
    /// <returns>The user ID, or null if the entity doesn't have a user ID.</returns>
    string? GetUserId(object entity);
}

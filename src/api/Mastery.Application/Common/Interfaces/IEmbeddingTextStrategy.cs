namespace Mastery.Application.Common.Interfaces;

/// <summary>
/// Strategy interface for compiling entity data into text suitable for embedding generation.
/// Each entity type has its own strategy that determines what context to include.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IEmbeddingTextStrategy<in T> where T : class
{
    /// <summary>
    /// Compiles the entity into text for embedding generation.
    /// Returns null if the entity should not be embedded (e.g., archived status).
    /// </summary>
    /// <param name="entity">The entity to compile.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The compiled text, or null if no embedding should be generated.</returns>
    Task<string?> CompileTextAsync(T entity, CancellationToken ct);
}

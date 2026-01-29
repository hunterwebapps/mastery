using Mastery.Application.Common.Models;

namespace Mastery.Application.Common.Interfaces;

/// <summary>
/// Vector storage service for embedding documents.
/// Handles storage, retrieval, and semantic search operations.
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// Upserts a single embedding document.
    /// </summary>
    /// <param name="document">The document to upsert.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpsertAsync(EmbeddingDocument document, CancellationToken ct);

    /// <summary>
    /// Upserts multiple embedding documents in a batch.
    /// </summary>
    /// <param name="documents">The documents to upsert.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpsertBatchAsync(IEnumerable<EmbeddingDocument> documents, CancellationToken ct);

    /// <summary>
    /// Deletes an embedding document by entity type and ID.
    /// </summary>
    /// <param name="entityType">The entity type.</param>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(string entityType, Guid entityId, CancellationToken ct);

    /// <summary>
    /// Searches for similar documents using vector similarity.
    /// </summary>
    /// <param name="userId">The user ID to scope the search to.</param>
    /// <param name="queryVector">The query embedding vector.</param>
    /// <param name="topK">Maximum number of results to return.</param>
    /// <param name="entityTypes">Optional filter for specific entity types.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The search results ordered by similarity.</returns>
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string userId,
        float[] queryVector,
        int topK = 10,
        IEnumerable<string>? entityTypes = null,
        CancellationToken ct = default);
}

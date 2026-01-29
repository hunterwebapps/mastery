namespace Mastery.Infrastructure.Outbox;

/// <summary>
/// Processes outbox entries to generate embeddings for downstream storage (e.g., Cosmos DB).
/// </summary>
public interface IEmbeddingProcessor
{
    /// <summary>
    /// Process a batch of deduplicated outbox entries.
    /// For Create/Update operations: queries entity by ID and generates embedding.
    /// For Delete operations: removes embedding from store.
    /// </summary>
    /// <param name="entries">Deduplicated outbox entries to process.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ProcessBatchAsync(IReadOnlyList<OutboxEntry> entries, CancellationToken ct);
}

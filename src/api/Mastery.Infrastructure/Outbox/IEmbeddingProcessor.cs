using Mastery.Infrastructure.Messaging.Events;

namespace Mastery.Infrastructure.Outbox;

/// <summary>
/// Processes entity change events to generate embeddings for downstream storage (e.g., Cosmos DB).
/// </summary>
public interface IEmbeddingProcessor
{
    /// <summary>
    /// Process a batch of deduplicated entity change events.
    /// For Create/Update operations: queries entity by ID and generates embedding.
    /// For Delete operations: removes embedding from store.
    /// </summary>
    /// <param name="events">Deduplicated entity change events to process.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ProcessBatchAsync(IReadOnlyList<EntityChangedEvent> events, CancellationToken ct);
}

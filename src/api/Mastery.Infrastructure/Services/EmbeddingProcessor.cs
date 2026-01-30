using Mastery.Application.Common.Interfaces;
using Mastery.Application.Common.Models;
using Mastery.Infrastructure.Messaging.Events;
using Mastery.Infrastructure.Outbox;
using Microsoft.Extensions.Logging;

namespace Mastery.Infrastructure.Services;

/// <summary>
/// Processes entity change events to generate embeddings and store them in Cosmos DB.
/// </summary>
public class EmbeddingProcessor(
    IEntityResolver _entityResolver,
    IEmbeddingTextStrategyFactory _strategyFactory,
    IEmbeddingService _embeddingService,
    IVectorStore _vectorStore,
    ILogger<EmbeddingProcessor> _logger)
    : IEmbeddingProcessor
{
    public async Task ProcessBatchAsync(IReadOnlyList<EntityChangedEvent> events, CancellationToken ct)
    {
        var deletions = new List<(string EntityType, Guid EntityId)>();
        var documentsToEmbed = new List<(EntityChangedEvent Event, object Entity, string Text)>();

        await ResolveEntitiesAsync(events, deletions, documentsToEmbed, ct);
        await GenerateAndStoreEmbeddingsAsync(documentsToEmbed, ct);
        await ProcessDeletionsAsync(deletions, ct);
    }

    private async Task ResolveEntitiesAsync(
        IReadOnlyList<EntityChangedEvent> events,
        List<(string EntityType, Guid EntityId)> deletions,
        List<(EntityChangedEvent Event, object Entity, string Text)> documentsToEmbed,
        CancellationToken ct)
    {
        foreach (var evt in events)
        {
            if (evt.Operation == "Deleted")
            {
                deletions.Add((evt.EntityType, evt.EntityId));
                continue;
            }

            try
            {
                var entity = await _entityResolver.ResolveAsync(evt.EntityType, evt.EntityId, ct);
                if (entity is null)
                {
                    _logger.LogError("Entity {EntityType}/{EntityId} not found, skipping",
                        evt.EntityType, evt.EntityId);
                    continue;
                }

                var text = await _strategyFactory.CompileTextAsync(evt.EntityType, entity, ct);
                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogError("No embedding text for {EntityType}/{EntityId} (likely archived/filtered)",
                        evt.EntityType, evt.EntityId);
                    continue;
                }

                documentsToEmbed.Add((evt, entity, text));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing entity {EntityType}/{EntityId}",
                    evt.EntityType, evt.EntityId);
            }
        }
    }

    private async Task GenerateAndStoreEmbeddingsAsync(
        List<(EntityChangedEvent Event, object Entity, string Text)> documentsToEmbed,
        CancellationToken ct)
    {
        if (documentsToEmbed.Count == 0)
            return;

        try
        {
            var texts = documentsToEmbed
                .Select(d => d.Text)
                .ToList();

            _logger.LogInformation("Generating embeddings for {Count} entities", texts.Count);

            var embeddings = await _embeddingService.GenerateBatchEmbeddingsAsync(texts, ct);
            var documents = BuildDocuments(documentsToEmbed, embeddings);

            if (documents.Count > 0)
            {
                await _vectorStore.UpsertBatchAsync(documents, ct);
                _logger.LogInformation("Upserted {Count} embeddings to Cosmos DB", documents.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating or storing embeddings");
            throw;
        }
    }

    private List<EmbeddingDocument> BuildDocuments(
        List<(EntityChangedEvent Event, object Entity, string Text)> documentsToEmbed,
        IReadOnlyList<float[]> embeddings)
    {
        var documents = new List<EmbeddingDocument>();

        for (var i = 0; i < documentsToEmbed.Count; i++)
        {
            var (evt, entity, text) = documentsToEmbed[i];
            var embedding = embeddings[i];

            if (embedding.Length == 0)
            {
                _logger.LogError("Empty embedding returned for {EntityType}/{EntityId}",
                    evt.EntityType, evt.EntityId);
                continue;
            }

            var userId = _strategyFactory.GetUserId(entity);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("Could not determine UserId for {EntityType}/{EntityId}",
                    evt.EntityType, evt.EntityId);
                continue;
            }

            documents.Add(CreateDocument(evt, text, embedding, userId));
        }

        return documents;
    }

    private async Task ProcessDeletionsAsync(
        List<(string EntityType, Guid EntityId)> deletions,
        CancellationToken ct)
    {
        foreach (var (entityType, entityId) in deletions)
        {
            try
            {
                await _vectorStore.DeleteAsync(entityType, entityId, ct);
                _logger.LogDebug("Deleted embedding for {EntityType}/{EntityId}", entityType, entityId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error deleting embedding for {EntityType}/{EntityId}",
                    entityType, entityId);
            }
        }
    }

    private static EmbeddingDocument CreateDocument(
        EntityChangedEvent evt,
        string embeddingText,
        float[] embedding,
        string userId)
    {
        return new EmbeddingDocument
        {
            Id = EmbeddingDocument.CreateId(evt.EntityType, evt.EntityId),
            UserId = userId,
            EntityType = evt.EntityType,
            EntityId = evt.EntityId,
            EmbeddingText = embeddingText,
            Embedding = embedding,
            UpdatedAt = DateTime.UtcNow,
        };
    }
}

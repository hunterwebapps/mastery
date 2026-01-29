using Mastery.Application.Common.Interfaces;
using Mastery.Application.Common.Models;
using Mastery.Infrastructure.Outbox;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mastery.Infrastructure.Services;

/// <summary>
/// Processes outbox entries to generate embeddings and store them in Cosmos DB.
/// </summary>
public class EmbeddingProcessor(
    IEntityResolver _entityResolver,
    IEmbeddingTextStrategyFactory _strategyFactory,
    IEmbeddingService _embeddingService,
    IVectorStore _vectorStore,
    ILogger<EmbeddingProcessor> _logger)
    : IEmbeddingProcessor
{
    public async Task ProcessBatchAsync(IReadOnlyList<OutboxEntry> entries, CancellationToken ct)
    {
        var deletions = new List<(string EntityType, Guid EntityId)>();
        var documentsToEmbed = new List<(OutboxEntry Entry, object Entity, string Text)>();

        await ResolveEntitiesAsync(entries, deletions, documentsToEmbed, ct);
        await GenerateAndStoreEmbeddingsAsync(documentsToEmbed, ct);
        await ProcessDeletionsAsync(deletions, ct);
    }

    private async Task ResolveEntitiesAsync(
        IReadOnlyList<OutboxEntry> entries,
        List<(string EntityType, Guid EntityId)> deletions,
        List<(OutboxEntry Entry, object Entity, string Text)> documentsToEmbed,
        CancellationToken ct)
    {
        foreach (var entry in entries)
        {
            if (entry.Operation == "Deleted")
            {
                deletions.Add((entry.EntityType, entry.EntityId));
                continue;
            }

            try
            {
                var entity = await _entityResolver.ResolveAsync(entry.EntityType, entry.EntityId, ct);
                if (entity is null)
                {
                    _logger.LogError("Entity {EntityType}/{EntityId} not found, skipping",
                        entry.EntityType, entry.EntityId);
                    continue;
                }

                var text = await _strategyFactory.CompileTextAsync(entry.EntityType, entity, ct);
                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogError("No embedding text for {EntityType}/{EntityId} (likely archived/filtered)",
                        entry.EntityType, entry.EntityId);
                    continue;
                }

                documentsToEmbed.Add((entry, entity, text));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing entity {EntityType}/{EntityId}",
                    entry.EntityType, entry.EntityId);
            }
        }
    }

    private async Task GenerateAndStoreEmbeddingsAsync(
        List<(OutboxEntry Entry, object Entity, string Text)> documentsToEmbed,
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
        List<(OutboxEntry Entry, object Entity, string Text)> documentsToEmbed,
        IReadOnlyList<float[]> embeddings)
    {
        var documents = new List<EmbeddingDocument>();

        for (var i = 0; i < documentsToEmbed.Count; i++)
        {
            var (entry, entity, text) = documentsToEmbed[i];
            var embedding = embeddings[i];

            if (embedding.Length == 0)
            {
                _logger.LogError("Empty embedding returned for {EntityType}/{EntityId}",
                    entry.EntityType, entry.EntityId);
                continue;
            }

            var userId = _strategyFactory.GetUserId(entity);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("Could not determine UserId for {EntityType}/{EntityId}",
                    entry.EntityType, entry.EntityId);
                continue;
            }

            documents.Add(CreateDocument(entry, text, embedding, userId));
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
        OutboxEntry entry,
        string embeddingText,
        float[] embedding,
        string userId)
    {
        return new EmbeddingDocument
        {
            Id = EmbeddingDocument.CreateId(entry.EntityType, entry.EntityId),
            UserId = userId,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            EmbeddingText = embeddingText,
            Embedding = embedding,
            UpdatedAt = DateTime.UtcNow,
        };
    }
}

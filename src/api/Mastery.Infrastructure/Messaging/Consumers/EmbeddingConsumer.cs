using DotNetCore.CAP;
using Mastery.Application.Common.Interfaces;
using Mastery.Application.Common.Models;
using Mastery.Domain.Enums;
using Mastery.Infrastructure.Messaging.Events;
using Mastery.Infrastructure.Messaging.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mastery.Infrastructure.Messaging.Consumers;

/// <summary>
/// CAP consumer that handles EntityChangedBatchEvent messages from the embeddings-pending queue.
/// Generates embeddings in batch and routes signals to appropriate queues.
/// </summary>
public sealed class EmbeddingConsumer(
    IEntityResolver _entityResolver,
    IEmbeddingTextStrategyFactory _strategyFactory,
    IEmbeddingService _embeddingService,
    IVectorStore _vectorStore,
    ISignalClassifier _signalClassifier,
    SignalRoutingService _signalRouter,
    ILogger<EmbeddingConsumer> _logger)
    : ICapSubscribe
{
    [CapSubscribe("embeddings-pending")]
    public async Task HandleBatchAsync(EntityChangedBatchEvent batch, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing batch of {Count} entity changes", batch.Events.Count);

        try
        {
            // Step 1: Handle deletions separately
            var deletions = batch.Events.Where(e => e.Operation == "Deleted").ToList();
            var nonDeletions = batch.Events.Where(e => e.Operation != "Deleted").ToList();

            foreach (var deletion in deletions)
            {
                await _vectorStore.DeleteAsync(deletion.EntityType, deletion.EntityId, cancellationToken);
            }

            if (deletions.Count > 0)
            {
                _logger.LogDebug("Deleted {Count} embeddings", deletions.Count);
            }

            if (nonDeletions.Count == 0)
            {
                return;
            }

            // Step 2: Resolve entities and prepare texts for batch embedding
            var documentsToEmbed = new List<(EntityChangedEvent Event, object Entity, string Text, string UserId)>();

            foreach (var evt in nonDeletions)
            {
                var entity = await _entityResolver.ResolveAsync(evt.EntityType, evt.EntityId, cancellationToken);
                if (entity is null)
                {
                    _logger.LogWarning("Entity {EntityType}/{EntityId} not found, skipping", evt.EntityType, evt.EntityId);
                    continue;
                }

                var text = await _strategyFactory.CompileTextAsync(evt.EntityType, entity, cancellationToken);
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                var userId = _strategyFactory.GetUserId(entity);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Could not determine UserId for {EntityType}/{EntityId}", evt.EntityType, evt.EntityId);
                    continue;
                }

                documentsToEmbed.Add((evt, entity, text, userId));
            }

            if (documentsToEmbed.Count == 0)
            {
                return;
            }

            // Step 3: Generate embeddings in batch
            var texts = documentsToEmbed.Select(d => d.Text).ToList();
            var embeddings = await _embeddingService.GenerateBatchEmbeddingsAsync(texts, cancellationToken);

            // Step 4: Store embeddings in batch
            var documents = new List<EmbeddingDocument>();
            for (var i = 0; i < documentsToEmbed.Count; i++)
            {
                var (evt, entity, text, userId) = documentsToEmbed[i];
                var embedding = embeddings[i];

                if (embedding.Length == 0)
                {
                    _logger.LogError("Empty embedding returned for {EntityType}/{EntityId}", evt.EntityType, evt.EntityId);
                    continue;
                }

                documents.Add(new EmbeddingDocument
                {
                    Id = EmbeddingDocument.CreateId(evt.EntityType, evt.EntityId),
                    UserId = userId,
                    EntityType = evt.EntityType,
                    EntityId = evt.EntityId,
                    EmbeddingText = text,
                    Embedding = embedding,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            if (documents.Count > 0)
            {
                await _vectorStore.UpsertBatchAsync(documents, cancellationToken);
                _logger.LogDebug("Stored {Count} embeddings", documents.Count);
            }

            // Step 5: Classify and batch route signals by priority
            await RouteBatchSignalsAsync(batch.Events, batch.CorrelationId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing embedding batch of {Count} events", batch.Events.Count);
            throw; // CAP will retry
        }
    }

    private async Task RouteBatchSignalsAsync(
        IReadOnlyList<EntityChangedEvent> events,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        // Group signals by priority and user for batch routing
        var signalsByPriorityAndUser = new Dictionary<(SignalPriority, string), List<SignalClassification>>();

        foreach (var evt in events)
        {
            if (string.IsNullOrEmpty(evt.DomainEventType) || string.IsNullOrEmpty(evt.UserId))
            {
                continue;
            }

            var classification = _signalClassifier.ClassifyOutboxEntry(
                evt.EntityType,
                evt.EntityId,
                evt.DomainEventType,
                evt.UserId);

            if (classification == null)
            {
                continue;
            }

            var key = (classification.Priority, evt.UserId);
            if (!signalsByPriorityAndUser.TryGetValue(key, out var list))
            {
                list = [];
                signalsByPriorityAndUser[key] = list;
            }
            list.Add(classification);
        }

        // Route each batch
        foreach (var ((priority, userId), classifications) in signalsByPriorityAndUser)
        {
            await _signalRouter.RouteSignalsAsync(
                classifications,
                priority,
                userId,
                correlationId,
                cancellationToken);
        }
    }
}

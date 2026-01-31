using Mastery.Application.Common.Interfaces;
using Mastery.Application.Common.Models;
using Mastery.Domain.Enums;
using Mastery.Infrastructure.Messaging.Events;
using Mastery.Infrastructure.Messaging.Services;
using Mastery.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace Mastery.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumer that handles EntityChangedBatchEvent messages from the embeddings-pending queue.
/// Generates embeddings in batch and routes signals to appropriate queues.
/// </summary>
public sealed class EmbeddingConsumer(
    IOptions<ServiceBusOptions> _options,
    IEntityResolver _entityResolver,
    IEmbeddingTextStrategyFactory _strategyFactory,
    IEmbeddingService _embeddingService,
    IVectorStore _vectorStore,
    ISignalClassifier _signalClassifier,
    SignalRoutingService _signalRouter,
    ILogger<EmbeddingConsumer> _logger)
    : IMessageHandler<EntityChangedBatchEvent>
{
    public string QueueName => _options.Value.EmbeddingsQueueName;

    public async Task HandleAsync(EntityChangedBatchEvent batch, CancellationToken cancellationToken)
    {
        using var activity = ActivityContextHelper.StartLinkedActivity(
            "ProcessEmbeddingBatch",
            batch.CorrelationId);

        using var prop1 = LogContext.PushProperty("CorrelationId", batch.CorrelationId ?? "unknown");
        using var prop2 = LogContext.PushProperty("BatchId", batch.BatchId);

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
                try
                {
                    var entity = await _entityResolver.ResolveAsync(evt.EntityType, evt.EntityId, cancellationToken);
                    if (entity is null)
                    {
                        _logger.LogError("Entity {EntityType}/{EntityId} not found, skipping", evt.EntityType, evt.EntityId);
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
                        _logger.LogError("Could not determine UserId for {EntityType}/{EntityId}", evt.EntityType, evt.EntityId);
                        continue;
                    }

                    documentsToEmbed.Add((evt, entity, text, userId));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing entity {EntityType}/{EntityId}", evt.EntityType, evt.EntityId);
                }
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
                    Id = evt.EntityId.ToString(),
                    UserId = userId,
                    EntityType = evt.EntityType,
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
            throw; // Service Bus will retry via abandon
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
            var classifications = evt.DomainEventTypes
                .Select(domainEventType => _signalClassifier.ClassifySignal(
                    evt.EntityType,
                    evt.EntityId,
                    domainEventType,
                    evt.UserId))
                .Where(classification => classification != null)
                .ToList();

            foreach (var classification in classifications)
            {
                var key = (classification!.Priority, evt.UserId);
                if (!signalsByPriorityAndUser.TryGetValue(key, out var list))
                {
                    list = [];
                    signalsByPriorityAndUser[key] = list;
                }
                list.Add(classification);
            }
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

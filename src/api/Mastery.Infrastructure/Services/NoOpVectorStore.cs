using Mastery.Application.Common.Interfaces;
using Mastery.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace Mastery.Infrastructure.Services;

/// <summary>
/// No-op implementation of IVectorStore used when Cosmos DB is disabled.
/// </summary>
public sealed class NoOpVectorStore : IVectorStore
{
    private readonly ILogger<NoOpVectorStore> _logger;

    public NoOpVectorStore(ILogger<NoOpVectorStore> logger)
    {
        _logger = logger;
    }

    public Task UpsertAsync(EmbeddingDocument document, CancellationToken ct)
    {
        _logger.LogDebug(
            "NoOp: Would upsert embedding for {EntityType}/{EntityId}",
            document.EntityType,
            document.EntityId);
        return Task.CompletedTask;
    }

    public Task UpsertBatchAsync(IEnumerable<EmbeddingDocument> documents, CancellationToken ct)
    {
        var count = documents.Count();
        _logger.LogDebug("NoOp: Would upsert {Count} embeddings", count);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string entityType, Guid entityId, CancellationToken ct)
    {
        _logger.LogDebug(
            "NoOp: Would delete embedding for {EntityType}/{EntityId}",
            entityType,
            entityId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string userId,
        float[] queryVector,
        int topK = 10,
        IEnumerable<string>? entityTypes = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug("NoOp: Would search embeddings for user {UserId}", userId);
        return Task.FromResult<IReadOnlyList<VectorSearchResult>>([]);
    }
}

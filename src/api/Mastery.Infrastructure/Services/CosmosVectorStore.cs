using Mastery.Application.Common.Interfaces;
using Mastery.Application.Common.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mastery.Infrastructure.Services;

/// <summary>
/// Cosmos DB implementation of vector storage with vector search capabilities.
/// </summary>
public sealed class CosmosVectorStore : IVectorStore
{
    private readonly Container _container;
    private readonly CosmosDbOptions _options;
    private readonly ILogger<CosmosVectorStore> _logger;

    public CosmosVectorStore(
        CosmosClient cosmosClient,
        IOptions<CosmosDbOptions> options,
        ILogger<CosmosVectorStore> logger)
    {
        _options = options.Value;
        _logger = logger;
        _container = cosmosClient.GetContainer(_options.DatabaseName, _options.ContainerName);
    }

    public async Task UpsertAsync(EmbeddingDocument document, CancellationToken ct)
    {
        try
        {
            await _container.UpsertItemAsync(
                document,
                new PartitionKey(document.UserId),
                cancellationToken: ct);

            _logger.LogDebug(
                "Upserted embedding for {EntityType}/{EntityId}",
                document.EntityType,
                document.Id);
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex,
                "Failed to upsert embedding for {EntityType}/{EntityId}",
                document.EntityType,
                document.Id);
            throw;
        }
    }

    public async Task UpsertBatchAsync(IEnumerable<EmbeddingDocument> documents, CancellationToken ct)
    {
        var documentList = documents.ToList();
        if (documentList.Count == 0)
        {
            return;
        }

        // Group by partition key (UserId) for batch operations
        var groups = documentList.GroupBy(d => d.UserId);

        foreach (var group in groups)
        {
            var batch = _container.CreateTransactionalBatch(new PartitionKey(group.Key));

            foreach (var doc in group)
            {
                batch.UpsertItem(doc);
            }

            try
            {
                using var response = await batch.ExecuteAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Batch upsert partially failed for user {UserId}: {StatusCode}",
                        group.Key,
                        response.StatusCode);
                }
                else
                {
                    _logger.LogDebug(
                        "Batch upserted {Count} embeddings for user {UserId}",
                        group.Count(),
                        group.Key);
                }
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex,
                    "Failed to batch upsert embeddings for user {UserId}",
                    group.Key);

                // Fall back to individual upserts
                foreach (var doc in group)
                {
                    try
                    {
                        await UpsertAsync(doc, ct);
                    }
                    catch (Exception innerEx)
                    {
                        _logger.LogError(innerEx,
                            "Individual upsert also failed for {EntityType}/{EntityId}",
                            doc.EntityType,
                            doc.Id);
                    }
                }
            }
        }
    }

    public async Task DeleteAsync(string entityType, Guid entityId, CancellationToken ct)
    {
        var documentId = EmbeddingDocument.CreateId(entityType, entityId);

        // We need to find the document first to get the partition key
        var query = new QueryDefinition(
            "SELECT c.userId FROM c WHERE c.id = @id")
            .WithParameter("@id", documentId);

        var iterator = _container.GetItemQueryIterator<dynamic>(query);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(ct);

            foreach (var item in response)
            {
                string userId = item.userId;

                try
                {
                    await _container.DeleteItemAsync<EmbeddingDocument>(
                        documentId,
                        new PartitionKey(userId),
                        cancellationToken: ct);

                    _logger.LogDebug(
                        "Deleted embedding for {EntityType}/{EntityId}",
                        entityType,
                        entityId);
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogError(ex,
                        "Embedding for {EntityType}/{EntityId} already deleted",
                        entityType,
                        entityId);
                }
            }
        }
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string userId,
        float[] queryVector,
        int topK = 10,
        IEnumerable<string>? entityTypes = null,
        CancellationToken ct = default)
    {
        var entityTypeFilter = entityTypes?.ToList();
        var hasEntityTypeFilter = entityTypeFilter?.Count > 0;

        // Build the vector search query using Cosmos DB's VectorDistance function
        var queryBuilder = new System.Text.StringBuilder();
        queryBuilder.Append("SELECT TOP @topK ");
        queryBuilder.Append("c.entityType, c.entityId, c.title, c.status, c.embeddingText, c.metadata, ");
        queryBuilder.Append("VectorDistance(c.embedding, @queryVector) AS score ");
        queryBuilder.Append("FROM c ");
        queryBuilder.Append("WHERE c.userId = @userId ");

        if (hasEntityTypeFilter)
        {
            queryBuilder.Append("AND c.entityType IN (");
            queryBuilder.Append(string.Join(", ", entityTypeFilter!.Select((_, i) => $"@entityType{i}")));
            queryBuilder.Append(") ");
        }

        queryBuilder.Append("ORDER BY VectorDistance(c.embedding, @queryVector)");

        var queryDefinition = new QueryDefinition(queryBuilder.ToString())
            .WithParameter("@topK", topK)
            .WithParameter("@userId", userId)
            .WithParameter("@queryVector", queryVector);

        if (hasEntityTypeFilter)
        {
            for (int i = 0; i < entityTypeFilter!.Count; i++)
            {
                queryDefinition = queryDefinition.WithParameter($"@entityType{i}", entityTypeFilter[i]);
            }
        }

        var results = new List<VectorSearchResult>();

        var iterator = _container.GetItemQueryIterator<VectorSearchResultInternal>(
            queryDefinition,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(userId)
            });

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(ct);

            foreach (var item in response)
            {
                results.Add(new VectorSearchResult
                {
                    EntityType = item.EntityType,
                    EntityId = item.EntityId,
                    Title = item.Title,
                    Status = item.Status,
                    EmbeddingText = item.EmbeddingText,
                    Score = 1 - item.Score, // Convert distance to similarity
                    Metadata = item.Metadata
                });
            }
        }

        _logger.LogDebug(
            "Vector search for user {UserId} returned {Count} results",
            userId,
            results.Count);

        return results;
    }

    /// <summary>
    /// Internal class for deserializing search results from Cosmos DB.
    /// </summary>
    private sealed class VectorSearchResultInternal
    {
        public string EntityType { get; set; } = null!;
        public Guid EntityId { get; set; }
        public string Title { get; set; } = null!;
        public string? Status { get; set; }
        public string EmbeddingText { get; set; } = null!;
        public double Score { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}

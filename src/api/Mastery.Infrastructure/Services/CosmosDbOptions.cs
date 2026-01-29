namespace Mastery.Infrastructure.Services;

/// <summary>
/// Configuration options for Cosmos DB vector store.
/// </summary>
public sealed class CosmosDbOptions
{
    public const string SectionName = "CosmosDb";

    /// <summary>
    /// Cosmos DB account endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Cosmos DB account key (for local development).
    /// In production, use managed identity instead.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Database name.
    /// </summary>
    public string DatabaseName { get; set; } = "mastery";

    /// <summary>
    /// Container name for embedding documents.
    /// </summary>
    public string ContainerName { get; set; } = "embeddings";

    /// <summary>
    /// Throughput in RUs (only used when creating container).
    /// </summary>
    public int ThroughputRUs { get; set; } = 400;
}

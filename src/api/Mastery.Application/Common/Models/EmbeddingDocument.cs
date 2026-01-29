using System.Text.Json.Serialization;

namespace Mastery.Application.Common.Models;

/// <summary>
/// Document model for Cosmos DB vector storage.
/// Partitioned by UserId for efficient user-scoped queries.
/// </summary>
public sealed class EmbeddingDocument
{
    /// <summary>
    /// Unique document ID in format "{EntityType}:{EntityId}".
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    /// <summary>
    /// Partition key - the user who owns this entity.
    /// </summary>
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = null!;

    /// <summary>
    /// The type of entity (e.g., "Goal", "Habit", "Task").
    /// </summary>
    [JsonPropertyName("entityType")]
    public string EntityType { get; set; } = null!;

    /// <summary>
    /// The entity's unique identifier.
    /// </summary>
    [JsonPropertyName("entityId")]
    public Guid EntityId { get; set; }

    /// <summary>
    /// The compiled text used to generate the embedding.
    /// Useful for debugging and result display.
    /// </summary>
    [JsonPropertyName("embeddingText")]
    public string EmbeddingText { get; set; } = null!;

    /// <summary>
    /// The embedding vector (3072 dimensions for text-embedding-3-large).
    /// </summary>
    [JsonPropertyName("embedding")]
    public float[] Embedding { get; set; } = null!;

    /// <summary>
    /// When the document was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Creates a document ID from entity type and ID.
    /// </summary>
    public static string CreateId(string entityType, Guid entityId)
        => $"{entityType}:{entityId}";
}

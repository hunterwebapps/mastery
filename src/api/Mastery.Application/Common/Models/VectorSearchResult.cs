namespace Mastery.Application.Common.Models;

/// <summary>
/// Result from a vector similarity search.
/// </summary>
public sealed class VectorSearchResult
{
    /// <summary>
    /// The entity type.
    /// </summary>
    public string EntityType { get; set; } = null!;

    /// <summary>
    /// The entity ID.
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// The entity title.
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// The entity status.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// The text that was embedded.
    /// </summary>
    public string EmbeddingText { get; set; } = null!;

    /// <summary>
    /// Cosine similarity score (0 to 1, higher is more similar).
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Additional metadata from the document.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

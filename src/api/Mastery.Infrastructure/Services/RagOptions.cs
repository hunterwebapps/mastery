namespace Mastery.Infrastructure.Services;

/// <summary>
/// Configuration options for RAG (Retrieval-Augmented Generation) in the LLM pipeline.
/// </summary>
public sealed class RagOptions
{
    public const string SectionName = "Rag";

    /// <summary>
    /// Minimum similarity score (0-1) for retrieved items to be included.
    /// Items below this threshold are filtered out.
    /// </summary>
    public double SimilarityThreshold { get; set; } = 0.5;

    /// <summary>
    /// Timeout in milliseconds for RAG retrieval operations.
    /// If exceeded, the pipeline continues without RAG context.
    /// </summary>
    public int TimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Maximum characters of embedding text to include in prompts per item.
    /// Longer texts are truncated to control token usage.
    /// </summary>
    public int MaxEmbeddingTextLength { get; set; } = 300;

    /// <summary>
    /// Stage-specific configuration for Assessment (Stage 1).
    /// </summary>
    public RagStageOptions Assessment { get; set; } = new()
    {
        TopK = 5,
        EntityTypes = ["CheckIn", "Experiment", "Recommendation"]
    };

    /// <summary>
    /// Stage-specific configuration for Strategy (Stage 2).
    /// </summary>
    public RagStageOptions Strategy { get; set; } = new()
    {
        TopK = 5,
        EntityTypes = ["Recommendation", "Experiment"]
    };

    /// <summary>
    /// Stage-specific configuration for Generation (Stage 3).
    /// EntityTypes is null to search all types; domain-specific filtering done in query.
    /// </summary>
    public RagStageOptions Generation { get; set; } = new()
    {
        TopK = 3,
        EntityTypes = null
    };
}

/// <summary>
/// Per-stage RAG configuration.
/// </summary>
public sealed class RagStageOptions
{
    /// <summary>
    /// Maximum number of items to retrieve from vector search.
    /// </summary>
    public int TopK { get; set; } = 5;

    /// <summary>
    /// Entity types to search. Null means search all types.
    /// </summary>
    public string[]? EntityTypes { get; set; }
}

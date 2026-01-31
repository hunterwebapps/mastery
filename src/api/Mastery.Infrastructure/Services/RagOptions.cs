namespace Mastery.Infrastructure.Services;

/// <summary>
/// Configuration options for RAG (Retrieval-Augmented Generation) in the LLM pipeline.
/// </summary>
public sealed class RagOptions
{
    public const string SectionName = "Rag";

    /// <summary>
    /// Enable agentic search in Stage 2 (Strategy).
    /// When enabled, the LLM can call a search_history tool to retrieve additional
    /// historical context beyond what was pre-fetched.
    /// Default is false - set to true to enable hybrid RAG.
    /// </summary>
    public bool EnableAgenticSearch { get; set; } = false;

    /// <summary>
    /// Maximum number of search_history tool calls allowed per Strategy stage.
    /// Limits latency impact from additional searches.
    /// </summary>
    public int MaxAgenticSearchCalls { get; set; } = 2;

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
    /// Increased TopK to 7 for richer historical context.
    /// Added Goal to entity types for goal progress patterns.
    /// </summary>
    public RagStageOptions Assessment { get; set; } = new()
    {
        TopK = 7,
        EntityTypes = ["CheckIn", "Experiment", "Recommendation", "Goal"]
    };

    /// <summary>
    /// Stage-specific configuration for Strategy (Stage 2).
    /// Increased TopK to 8 for more intervention history.
    /// </summary>
    public RagStageOptions Strategy { get; set; } = new()
    {
        TopK = 8,
        EntityTypes = ["Recommendation", "Experiment"]
    };

    /// <summary>
    /// Default configuration for Generation (Stage 3).
    /// Used when no domain-specific config exists.
    /// </summary>
    public RagStageOptions Generation { get; set; } = new()
    {
        TopK = 4,
        EntityTypes = null
    };

    /// <summary>
    /// Domain-specific configuration for Generation (Stage 3).
    /// Each domain has tailored entity types for more relevant context.
    /// </summary>
    public Dictionary<string, RagStageOptions> GenerationByDomain { get; set; } = new()
    {
        ["Task"] = new RagStageOptions { TopK = 4, EntityTypes = ["Task", "Recommendation"] },
        ["Habit"] = new RagStageOptions { TopK = 4, EntityTypes = ["Habit", "Recommendation", "Experiment"] },
        ["Experiment"] = new RagStageOptions { TopK = 5, EntityTypes = ["Experiment", "Recommendation"] },
        ["GoalMetric"] = new RagStageOptions { TopK = 4, EntityTypes = ["Goal", "MetricDefinition", "Recommendation"] },
        ["Project"] = new RagStageOptions { TopK = 4, EntityTypes = ["Project", "Task", "Recommendation"] }
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

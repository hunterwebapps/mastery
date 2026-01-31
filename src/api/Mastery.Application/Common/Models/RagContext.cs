namespace Mastery.Application.Common.Models;

/// <summary>
/// RAG context retrieved for a specific pipeline stage.
/// </summary>
public sealed record RagContext(
    RagContextStage Stage,
    IReadOnlyList<RagContextItem> Items,
    string QueryText,
    TimeSpan RetrievalLatency);

/// <summary>
/// A single item retrieved from the vector store via RAG.
/// </summary>
public sealed record RagContextItem(
    string EntityType,
    Guid EntityId,
    string Title,
    string? Status,
    string EmbeddingText,
    double SimilarityScore);

/// <summary>
/// Identifies which pipeline stage the RAG context was retrieved for.
/// </summary>
public enum RagContextStage
{
    /// <summary>
    /// Stage 1: Situational Assessment - retrieves historical check-ins, experiments, and recommendations.
    /// </summary>
    Assessment,

    /// <summary>
    /// Stage 2: Strategy Planning - retrieves past recommendations and experiments.
    /// </summary>
    Strategy,

    /// <summary>
    /// Stage 3: Domain Generation - retrieves domain-specific historical context.
    /// </summary>
    Generation
}

namespace Mastery.Application.Common.Interfaces;

/// <summary>
/// Service for generating text embeddings using an embedding model.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generates an embedding vector for the given text.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The embedding vector.</returns>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct);

    /// <summary>
    /// Generates embedding vectors for multiple texts in a single batch.
    /// More efficient than calling GenerateEmbeddingAsync multiple times.
    /// </summary>
    /// <param name="texts">The texts to embed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The embedding vectors in the same order as the input texts.</returns>
    Task<IReadOnlyList<float[]>> GenerateBatchEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken ct);
}

namespace Mastery.Infrastructure.Services.OpenAi;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAi";

    public string ApiKey { get; set; } = string.Empty;
    public string ModelName { get; set; } = "gpt-5-mini";
    public int MaxOutputTokens { get; set; } = 4096;
    public int TimeoutSeconds { get; set; } = 30;
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Model name for text embeddings.
    /// </summary>
    public string EmbeddingModel { get; set; } = "text-embedding-3-large";

    /// <summary>
    /// Embedding vector dimensions.
    /// text-embedding-3-large supports up to 3072 dimensions.
    /// </summary>
    public int EmbeddingDimensions { get; set; } = 3072;
}

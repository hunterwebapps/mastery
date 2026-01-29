using System.ClientModel;
using Mastery.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Embeddings;

namespace Mastery.Infrastructure.Services;

/// <summary>
/// Generates text embeddings using OpenAI's embedding models.
/// </summary>
public sealed class OpenAiEmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _client;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiEmbeddingService> _logger;

    public OpenAiEmbeddingService(
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiEmbeddingService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var openAiClient = new OpenAIClient(new ApiKeyCredential(_options.ApiKey));
        _client = openAiClient.GetEmbeddingClient("text-embedding-3-large");
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text cannot be empty", nameof(text));
        }

        var embeddingOptions = new EmbeddingGenerationOptions
        {
            Dimensions = _options.EmbeddingDimensions
        };

        var response = await _client.GenerateEmbeddingAsync(text, embeddingOptions, ct);

        return response.Value.ToFloats().ToArray();
    }

    public async Task<IReadOnlyList<float[]>> GenerateBatchEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken ct)
    {
        if (texts.Count == 0)
        {
            return [];
        }

        // Filter out empty texts but track indices
        var validIndices = new List<int>();
        var validTexts = new List<string>();
        for (int i = 0; i < texts.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(texts[i]))
            {
                validIndices.Add(i);
                validTexts.Add(texts[i]);
            }
        }

        if (validTexts.Count == 0)
        {
            return texts.Select(_ => Array.Empty<float>()).ToList();
        }

        var embeddingOptions = new EmbeddingGenerationOptions
        {
            Dimensions = _options.EmbeddingDimensions,
        };

        _logger.LogDebug("Generating embeddings for {Count} texts", validTexts.Count);

        var response = await _client.GenerateEmbeddingsAsync(validTexts, embeddingOptions, ct);

        // Map results back to original indices
        var results = new float[texts.Count][];
        for (int i = 0; i < texts.Count; i++)
        {
            results[i] = [];
        }

        var embeddings = response.Value;
        for (int i = 0; i < embeddings.Count; i++)
        {
            var originalIndex = validIndices[i];
            results[originalIndex] = embeddings[i].ToFloats().ToArray();
        }

        _logger.LogDebug("Generated {Count} embeddings", validTexts.Count);

        return results;
    }
}

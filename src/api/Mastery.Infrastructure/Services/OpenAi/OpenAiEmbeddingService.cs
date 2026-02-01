using System.ClientModel;
using System.Diagnostics;
using Mastery.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Embeddings;

namespace Mastery.Infrastructure.Services.OpenAi;

/// <summary>
/// Generates text embeddings using OpenAI's embedding models.
/// </summary>
public sealed class OpenAiEmbeddingService : IEmbeddingService
{
    private const string EmbeddingModel = "text-embedding-3-large";

    private readonly EmbeddingClient _client;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiEmbeddingService> _logger;
    private readonly ILlmMetricsCollector _metricsCollector;

    public OpenAiEmbeddingService(
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiEmbeddingService> logger,
        ILlmMetricsCollector metricsCollector)
    {
        this._options = options.Value;
        this._logger = logger;
        this._metricsCollector = metricsCollector;

        var openAiClient = new OpenAIClient(new ApiKeyCredential(this._options.ApiKey));
        this._client = openAiClient.GetEmbeddingClient(EmbeddingModel);
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text cannot be empty", nameof(text));
        }

        var stopwatch = Stopwatch.StartNew();

        var embeddingOptions = new EmbeddingGenerationOptions
        {
            Dimensions = this._options.EmbeddingDimensions
        };

        var response = await this._client.GenerateEmbeddingAsync(text, embeddingOptions, ct);
        stopwatch.Stop();

        // Record metrics - single embedding doesn't expose usage, estimate from text length
        // Average token is ~4 characters for English text
        var estimatedTokens = (text.Length + 3) / 4;
        this._metricsCollector.RecordEmbeddingCall(
            EmbeddingModel,
            estimatedTokens,
            itemCount: 1,
            (int)stopwatch.ElapsedMilliseconds);

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

        var stopwatch = Stopwatch.StartNew();

        var embeddingOptions = new EmbeddingGenerationOptions
        {
            Dimensions = this._options.EmbeddingDimensions,
        };

        this._logger.LogDebug("Generating embeddings for {Count} texts", validTexts.Count);

        var response = await this._client.GenerateEmbeddingsAsync(validTexts, embeddingOptions, ct);
        stopwatch.Stop();

        // Record metrics - sum up total tokens from all embeddings
        var totalTokens = response.Value.Usage?.TotalTokenCount ?? 0;
        this._metricsCollector.RecordEmbeddingCall(
            EmbeddingModel,
            totalTokens,
            validTexts.Count,
            (int)stopwatch.ElapsedMilliseconds);

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

        this._logger.LogDebug("Generated {Count} embeddings, {Tokens} tokens",
            validTexts.Count,
            totalTokens);

        return results;
    }
}

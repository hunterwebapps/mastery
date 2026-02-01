using Mastery.Domain.Common;

namespace Mastery.Domain.Entities.Recommendation;

/// <summary>
/// Tracks an individual LLM call within a recommendation pipeline run.
/// Stores metadata for debugging, cost tracking, and performance analysis.
/// </summary>
[EmbeddingIgnore]
public sealed class AgentRun : BaseEntity
{
    /// <summary>
    /// The trace this run belongs to.
    /// </summary>
    public Guid RecommendationTraceId { get; private set; }

    /// <summary>
    /// The user being processed when this LLM call was made.
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// The stage within the pipeline (e.g., "Assessment", "Selection").
    /// </summary>
    public string Stage { get; private set; } = null!;

    /// <summary>
    /// The model used for this call (e.g., "gpt-5-mini").
    /// </summary>
    public string Model { get; private set; } = null!;

    /// <summary>
    /// Number of input tokens sent to the model.
    /// </summary>
    public int InputTokens { get; private set; }

    /// <summary>
    /// Number of output tokens received from the model.
    /// </summary>
    public int OutputTokens { get; private set; }

    /// <summary>
    /// Number of input tokens served from prompt cache (reduces cost).
    /// </summary>
    public int? CachedInputTokens { get; private set; }

    /// <summary>
    /// Number of reasoning tokens used by reasoning models (o1/o3).
    /// </summary>
    public int? ReasoningTokens { get; private set; }

    /// <summary>
    /// Total latency in milliseconds for this LLM call.
    /// </summary>
    public int LatencyMs { get; private set; }

    /// <summary>
    /// Error type if the call failed (null if successful).
    /// </summary>
    public string? ErrorType { get; private set; }

    /// <summary>
    /// Error message if the call failed (null if successful).
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Number of retry attempts before success or final failure.
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// When this LLM call started.
    /// </summary>
    public DateTime StartedAt { get; private set; }

    /// <summary>
    /// When this LLM call completed.
    /// </summary>
    public DateTime CompletedAt { get; private set; }

    /// <summary>
    /// OpenAI backend fingerprint for reproducibility debugging.
    /// </summary>
    public string? SystemFingerprint { get; private set; }

    /// <summary>
    /// Provider request ID for debugging and support escalation.
    /// </summary>
    public string? RequestId { get; private set; }

    /// <summary>
    /// LLM provider name (e.g., "OpenAI", "Anthropic").
    /// </summary>
    public string? Provider { get; private set; }

    private AgentRun() { } // EF Core

    public static AgentRun CreateSuccessful(
        Guid recommendationTraceId,
        string stage,
        string model,
        int inputTokens,
        int outputTokens,
        int latencyMs,
        DateTime startedAt,
        DateTime completedAt,
        int retryCount,
        Guid userId,
        int cachedInputTokens,
        int reasoningTokens,
        string systemFingerprint,
        string requestId,
        string provider)
    {
        return new AgentRun
        {
            RecommendationTraceId = recommendationTraceId,
            UserId = userId,
            Stage = stage,
            Model = model,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            CachedInputTokens = cachedInputTokens,
            ReasoningTokens = reasoningTokens,
            LatencyMs = latencyMs,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            RetryCount = retryCount,
            SystemFingerprint = systemFingerprint,
            RequestId = requestId,
            Provider = provider
        };
    }

    public static AgentRun CreateFailed(
        Guid recommendationTraceId,
        string stage,
        string model,
        int inputTokens,
        int latencyMs,
        string errorType,
        string? errorMessage,
        DateTime startedAt,
        DateTime completedAt,
        int retryCount,
        Guid userId,
        int cachedInputTokens,
        string systemFingerprint,
        string requestId,
        string provider)
    {
        return new AgentRun
        {
            RecommendationTraceId = recommendationTraceId,
            UserId = userId,
            Stage = stage,
            Model = model,
            InputTokens = inputTokens,
            OutputTokens = 0,
            CachedInputTokens = cachedInputTokens,
            LatencyMs = latencyMs,
            ErrorType = errorType,
            ErrorMessage = errorMessage?.Length > 500 ? errorMessage[..500] : errorMessage,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            RetryCount = retryCount,
            SystemFingerprint = systemFingerprint,
            RequestId = requestId,
            Provider = provider
        };
    }
}

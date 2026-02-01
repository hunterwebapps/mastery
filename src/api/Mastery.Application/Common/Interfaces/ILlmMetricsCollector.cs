using Mastery.Application.Common.Models;

namespace Mastery.Application.Common.Interfaces;

/// <summary>
/// Collects LLM and embedding call metrics for cost tracking and debugging.
/// Implementations can persist to database, log, or send to monitoring systems.
/// </summary>
public interface ILlmMetricsCollector
{
    /// <summary>
    /// Records an LLM or embedding call.
    /// </summary>
    /// <param name="callRecord">The call metadata.</param>
    /// <param name="userId">The user being processed (if applicable).</param>
    /// <param name="traceId">Optional trace ID for correlation.</param>
    void RecordCall(LlmCallRecord callRecord, Guid? userId = null, Guid? traceId = null);

    /// <summary>
    /// Records an embedding batch call.
    /// </summary>
    /// <param name="model">The embedding model used.</param>
    /// <param name="totalTokens">Total tokens processed.</param>
    /// <param name="itemCount">Number of items in the batch.</param>
    /// <param name="latencyMs">Total latency in milliseconds.</param>
    /// <param name="userId">The user being processed (if applicable).</param>
    void RecordEmbeddingCall(
        string model,
        int totalTokens,
        int itemCount,
        int latencyMs,
        Guid? userId = null);
}

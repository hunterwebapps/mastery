using Mastery.Application.Common.Interfaces;
using Mastery.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace Mastery.Infrastructure.Services;

/// <summary>
/// Collects and logs LLM metrics. Can be extended to persist to database.
/// </summary>
public sealed class LlmMetricsCollector(ILogger<LlmMetricsCollector> logger) : ILlmMetricsCollector
{
    public void RecordCall(LlmCallRecord callRecord, Guid? userId = null, Guid? traceId = null)
    {
        if (callRecord.ErrorType is not null)
        {
            logger.LogWarning(
                "LLM call failed: Stage={Stage} Model={Model} Error={ErrorType} UserId={UserId} LatencyMs={LatencyMs}",
                callRecord.Stage,
                callRecord.Model,
                callRecord.ErrorType,
                userId,
                callRecord.LatencyMs);
        }
        else
        {
            logger.LogInformation(
                "LLM call: Stage={Stage} Model={Model} UserId={UserId} " +
                "InputTokens={InputTokens} OutputTokens={OutputTokens} CachedTokens={CachedTokens} ReasoningTokens={ReasoningTokens} " +
                "LatencyMs={LatencyMs} RequestId={RequestId}",
                callRecord.Stage,
                callRecord.Model,
                userId,
                callRecord.InputTokens,
                callRecord.OutputTokens,
                callRecord.CachedInputTokens,
                callRecord.ReasoningTokens,
                callRecord.LatencyMs,
                callRecord.RequestId);
        }
    }

    public void RecordEmbeddingCall(
        string model,
        int totalTokens,
        int itemCount,
        int latencyMs,
        Guid? userId = null)
    {
        logger.LogInformation(
            "Embedding call: Model={Model} UserId={UserId} Tokens={Tokens} Items={Items} LatencyMs={LatencyMs}",
            model,
            userId,
            totalTokens,
            itemCount,
            latencyMs);
    }
}

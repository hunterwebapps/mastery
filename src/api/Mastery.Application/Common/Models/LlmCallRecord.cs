namespace Mastery.Application.Common.Models;

/// <summary>
/// Captures metadata from an LLM API call for cost tracking and debugging.
/// This is a DTO used to transfer data between infrastructure and application layers.
/// </summary>
public sealed record LlmCallRecord(
    string Stage,
    string Model,
    int InputTokens,
    int OutputTokens,
    int LatencyMs,
    DateTime StartedAt,
    DateTime CompletedAt,
    int CachedInputTokens,
    int ReasoningTokens,
    string SystemFingerprint,
    string RequestId,
    string Provider,
    string? ErrorType = null,
    string? ErrorMessage = null);

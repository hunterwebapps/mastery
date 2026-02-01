namespace Mastery.Application.Features.Recommendations.Models;

/// <summary>
/// Summary DTO for admin trace list view.
/// </summary>
public sealed record AdminTraceListDto
{
    public Guid Id { get; init; }
    public Guid RecommendationId { get; init; }
    public required string UserId { get; init; }
    public required string UserEmail { get; init; }
    public required string RecommendationType { get; init; }
    public required string RecommendationStatus { get; init; }
    public required string Context { get; init; }
    public required string SelectionMethod { get; init; }
    public int FinalTier { get; init; }
    public required string ProcessingWindowType { get; init; }
    public int TotalDurationMs { get; init; }
    public int TotalTokens { get; init; }
    public int AgentRunCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Full detail DTO for admin trace inspection.
/// Includes decompressed JSON objects for debugging.
/// </summary>
public sealed record AdminTraceDetailDto
{
    public Guid Id { get; init; }
    public Guid RecommendationId { get; init; }
    public required string UserId { get; init; }
    public required string UserEmail { get; init; }

    // Recommendation details
    public required string RecommendationType { get; init; }
    public required string RecommendationStatus { get; init; }
    public required string RecommendationTitle { get; init; }
    public required string RecommendationRationale { get; init; }
    public required string Context { get; init; }
    public decimal RecommendationScore { get; init; }

    // Trace metadata
    public required string SelectionMethod { get; init; }
    public string? PromptVersion { get; init; }
    public string? ModelVersion { get; init; }
    public int FinalTier { get; init; }
    public required string ProcessingWindowType { get; init; }
    public int TotalDurationMs { get; init; }

    // Decompressed JSON objects (displayed as structured data in UI)
    public object? StateSnapshot { get; init; }
    public object? SignalsSummary { get; init; }
    public object? CandidateList { get; init; }
    public object? Tier0TriggeredRules { get; init; }
    public object? Tier1Scores { get; init; }
    public string? Tier1EscalationReason { get; init; }
    public object? PolicyResult { get; init; }
    public string? RawLlmResponse { get; init; }

    // Agent runs for this trace
    public IReadOnlyList<AgentRunDto> AgentRuns { get; init; } = [];

    // Timestamps
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
}

/// <summary>
/// DTO for LLM call details in a recommendation trace.
/// </summary>
public sealed record AgentRunDto
{
    public Guid Id { get; init; }
    public required string Stage { get; init; }
    public required string Model { get; init; }
    public string? Provider { get; init; }
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public int? CachedInputTokens { get; init; }
    public int? ReasoningTokens { get; init; }
    public int TotalTokens => InputTokens + OutputTokens + (ReasoningTokens ?? 0);
    public int LatencyMs { get; init; }
    public bool IsSuccess => string.IsNullOrEmpty(ErrorType);
    public string? ErrorType { get; init; }
    public string? ErrorMessage { get; init; }
    public int RetryCount { get; init; }
    public string? SystemFingerprint { get; init; }
    public string? RequestId { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime CompletedAt { get; init; }
}

/// <summary>
/// Filter parameters for admin trace listing.
/// </summary>
public sealed record AdminTraceFilterParams
{
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public string? Context { get; init; }
    public string? Status { get; init; }
    public string? UserId { get; init; }
    public string? SelectionMethod { get; init; }
    public int? FinalTier { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

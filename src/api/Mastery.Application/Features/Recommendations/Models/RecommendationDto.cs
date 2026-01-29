namespace Mastery.Application.Features.Recommendations.Models;

/// <summary>
/// Full recommendation DTO with trace information.
/// </summary>
public sealed record RecommendationDto
{
    public Guid Id { get; init; }
    public required string UserId { get; init; }
    public required string Type { get; init; }
    public required string Status { get; init; }
    public required string Context { get; init; }
    public required string TargetKind { get; init; }
    public Guid? TargetEntityId { get; init; }
    public string? TargetEntityTitle { get; init; }
    public required string ActionKind { get; init; }
    public required string Title { get; init; }
    public required string Rationale { get; init; }
    public string? ActionPayload { get; init; }
    public string? ActionSummary { get; init; }
    public decimal Score { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? RespondedAt { get; init; }
    public string? DismissReason { get; init; }
    public List<Guid> SignalIds { get; init; } = [];
    public RecommendationTraceDto? Trace { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
}

/// <summary>
/// Lightweight recommendation summary for list views.
/// </summary>
public sealed record RecommendationSummaryDto
{
    public Guid Id { get; init; }
    public required string Type { get; init; }
    public required string Status { get; init; }
    public required string Context { get; init; }
    public required string TargetKind { get; init; }
    public Guid? TargetEntityId { get; init; }
    public string? TargetEntityTitle { get; init; }
    public required string ActionKind { get; init; }
    public required string Title { get; init; }
    public required string Rationale { get; init; }
    public string? ActionSummary { get; init; }
    public decimal Score { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Recommendation trace for auditability.
/// </summary>
public sealed record RecommendationTraceDto
{
    public Guid Id { get; init; }
    public string? StateSnapshotJson { get; init; }
    public string? SignalsSummaryJson { get; init; }
    public string? CandidateListJson { get; init; }
    public string? PromptVersion { get; init; }
    public string? ModelVersion { get; init; }
    public string? RawLlmResponse { get; init; }
    public required string SelectionMethod { get; init; }
}

/// <summary>
/// Diagnostic signal DTO.
/// </summary>
public sealed record DiagnosticSignalDto
{
    public Guid Id { get; init; }
    public required string Type { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public int Severity { get; init; }
    public required string EvidenceMetric { get; init; }
    public decimal EvidenceCurrentValue { get; init; }
    public decimal? EvidenceThresholdValue { get; init; }
    public string? EvidenceDetail { get; init; }
    public DateOnly DetectedOn { get; init; }
    public bool IsActive { get; init; }
}

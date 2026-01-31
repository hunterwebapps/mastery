using Mastery.Domain.Enums;

namespace Mastery.Domain.Diagnostics;

/// <summary>
/// A recommendation that can be generated directly from a rule without LLM involvement.
/// </summary>
public sealed record DirectRecommendationCandidate(
    RecommendationType Type,
    RecommendationContext Context,
    RecommendationTargetKind TargetKind,
    Guid? TargetEntityId,
    string? TargetEntityTitle,
    RecommendationActionKind ActionKind,
    string Title,
    string Rationale,
    decimal Score,
    string? ActionPayload = null,
    string? ActionSummary = null);

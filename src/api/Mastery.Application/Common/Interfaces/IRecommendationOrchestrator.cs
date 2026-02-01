using Mastery.Application.Common.Models;
using Mastery.Domain.Diagnostics;
using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Enums;

namespace Mastery.Application.Common.Interfaces;

public sealed record RecommendationOrchestrationResult(
    IReadOnlyList<RecommendationCandidate> SelectedCandidates,
    string SelectionMethod,
    string? PromptVersion = null,
    string? ModelVersion = null,
    string? RawResponse = null,
    IReadOnlyList<LlmCallRecord>? LlmCalls = null);

/// <summary>
/// Orchestrates the LLM-driven recommendation pipeline:
/// Stage 1 (Assessment) → Stage 2 (Selection from Tier 0 candidates)
/// The LLM selects from pre-computed deterministic candidates and provides rationale.
/// </summary>
public interface IRecommendationOrchestrator
{
    /// <summary>
    /// Orchestrates the recommendation selection process.
    /// </summary>
    /// <param name="state">User's current state snapshot.</param>
    /// <param name="context">The recommendation context (morning, evening, weekly, etc.).</param>
    /// <param name="candidates">Pre-computed candidates from Tier 0 deterministic rules.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Selected recommendations with LLM-generated rationale.</returns>
    Task<RecommendationOrchestrationResult> OrchestrateAsync(
        UserStateSnapshot state,
        RecommendationContext context,
        IReadOnlyList<DirectRecommendationCandidate> candidates,
        CancellationToken cancellationToken = default);
}

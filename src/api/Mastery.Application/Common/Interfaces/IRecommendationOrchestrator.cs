using Mastery.Application.Common.Models;
using Mastery.Domain.Enums;

namespace Mastery.Application.Common.Interfaces;

public sealed record RecommendationOrchestrationResult(
    IReadOnlyList<RecommendationCandidate> SelectedCandidates,
    string SelectionMethod,
    string? PromptVersion = null,
    string? ModelVersion = null,
    string? RawResponse = null);

/// <summary>
/// Orchestrates the LLM-driven recommendation pipeline:
/// Stage 1 (Assessment) → Stage 2 (Strategy) → Stage 3 (Domain Generation)
/// </summary>
public interface IRecommendationOrchestrator
{
    Task<RecommendationOrchestrationResult> OrchestrateAsync(
        UserStateSnapshot state,
        RecommendationContext context,
        CancellationToken cancellationToken = default);
}

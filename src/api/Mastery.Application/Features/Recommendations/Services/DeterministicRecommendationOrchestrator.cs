using Mastery.Application.Common.Interfaces;
using Mastery.Application.Common.Models;
using Mastery.Domain.Enums;

namespace Mastery.Application.Features.Recommendations.Services;

/// <summary>
/// Phase 1 implementation: returns ranked candidates as-is without LLM involvement.
/// Swap in a real LLM orchestrator via DI when ready.
/// </summary>
public sealed class DeterministicRecommendationOrchestrator : IRecommendationOrchestrator
{
    public Task<RecommendationOrchestrationResult> OrchestrateAsync(
        IReadOnlyList<RecommendationCandidate> rankedCandidates,
        UserStateSnapshot state,
        IReadOnlyList<Domain.Entities.Recommendation.DiagnosticSignal> signals,
        RecommendationContext context,
        CancellationToken cancellationToken = default)
    {
        var result = new RecommendationOrchestrationResult(
            SelectedCandidates: rankedCandidates,
            SelectionMethod: "Deterministic");

        return Task.FromResult(result);
    }
}

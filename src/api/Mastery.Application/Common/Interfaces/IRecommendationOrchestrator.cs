using Mastery.Application.Common.Models;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;

namespace Mastery.Application.Common.Interfaces;

public sealed record RecommendationOrchestrationResult(
    IReadOnlyList<RecommendationCandidate> SelectedCandidates,
    string SelectionMethod,
    string? PromptVersion = null,
    string? ModelVersion = null,
    string? RawResponse = null);

public interface IRecommendationOrchestrator
{
    Task<RecommendationOrchestrationResult> OrchestrateAsync(
        IReadOnlyList<RecommendationCandidate> rankedCandidates,
        UserStateSnapshot state,
        IReadOnlyList<DiagnosticSignal> signals,
        RecommendationContext context,
        CancellationToken cancellationToken = default);
}

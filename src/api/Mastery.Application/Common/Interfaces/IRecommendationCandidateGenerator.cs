using Mastery.Application.Common.Models;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;

namespace Mastery.Application.Common.Interfaces;

public interface IRecommendationCandidateGenerator
{
    IReadOnlyList<RecommendationType> SupportedTypes { get; }

    Task<IReadOnlyList<RecommendationCandidate>> GenerateAsync(
        UserStateSnapshot state,
        IReadOnlyList<DiagnosticSignal> signals,
        RecommendationContext context,
        CancellationToken cancellationToken = default);
}

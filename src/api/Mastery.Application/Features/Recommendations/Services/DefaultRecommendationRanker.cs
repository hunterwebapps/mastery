using Mastery.Application.Common.Interfaces;
using Mastery.Application.Common.Models;
using Mastery.Domain.Enums;

namespace Mastery.Application.Features.Recommendations.Services;

public sealed class DefaultRecommendationRanker : IRecommendationRanker
{
    public IReadOnlyList<RecommendationCandidate> Rank(
        IReadOnlyList<RecommendationCandidate> candidates,
        UserStateSnapshot state,
        int maxResults = 5)
    {
        if (candidates.Count == 0)
            return [];

        // Deduplicate by target entity (keep highest-scoring per entity)
        var deduped = candidates
            .GroupBy(c => new { c.Target.Kind, c.Target.EntityId })
            .Select(g => g.OrderByDescending(c => c.Score).First())
            .ToList();

        // Sort by score descending
        var ranked = deduped
            .OrderByDescending(c => c.Score)
            .Take(maxResults)
            .ToList();

        return ranked;
    }
}

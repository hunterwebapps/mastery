using Mastery.Application.Common.Models;
using Mastery.Domain.Enums;

namespace Mastery.Application.Common.Interfaces;

public interface IRecommendationRanker
{
    IReadOnlyList<RecommendationCandidate> Rank(
        IReadOnlyList<RecommendationCandidate> candidates,
        UserStateSnapshot state,
        int maxResults = 5);
}

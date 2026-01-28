using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Recommendations.Models;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Recommendations.Queries.GetActiveRecommendations;

public sealed class GetActiveRecommendationsQueryHandler
    : IQueryHandler<GetActiveRecommendationsQuery, IReadOnlyList<RecommendationSummaryDto>>
{
    private readonly IRecommendationRepository _recommendationRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetActiveRecommendationsQueryHandler(
        IRecommendationRepository recommendationRepository,
        ICurrentUserService currentUserService)
    {
        _recommendationRepository = recommendationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<RecommendationSummaryDto>> Handle(
        GetActiveRecommendationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return [];

        if (!string.IsNullOrEmpty(request.Context) &&
            Enum.TryParse<RecommendationContext>(request.Context, out var context))
        {
            var contextRecs = await _recommendationRepository.GetByUserIdAndContextAsync(userId, context, cancellationToken);
            return contextRecs.Select(r => r.ToSummaryDto()).ToList();
        }

        var recs = await _recommendationRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        return recs.Select(r => r.ToSummaryDto()).ToList();
    }
}

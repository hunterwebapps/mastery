using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Recommendations.Models;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Recommendations.Queries.GetRecommendationHistory;

public sealed class GetRecommendationHistoryQueryHandler
    : IQueryHandler<GetRecommendationHistoryQuery, IReadOnlyList<RecommendationSummaryDto>>
{
    private readonly IRecommendationRepository _recommendationRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetRecommendationHistoryQueryHandler(
        IRecommendationRepository recommendationRepository,
        ICurrentUserService currentUserService)
    {
        _recommendationRepository = recommendationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<RecommendationSummaryDto>> Handle(
        GetRecommendationHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return [];

        var recs = await _recommendationRepository.GetHistoryAsync(
            userId, request.FromDate, request.ToDate, cancellationToken);

        return recs.Select(r => r.ToSummaryDto()).ToList();
    }
}

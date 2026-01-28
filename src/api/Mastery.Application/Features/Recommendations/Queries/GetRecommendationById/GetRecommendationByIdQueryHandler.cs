using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Recommendations.Models;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Recommendations.Queries.GetRecommendationById;

public sealed class GetRecommendationByIdQueryHandler
    : IQueryHandler<GetRecommendationByIdQuery, RecommendationDto>
{
    private readonly IRecommendationRepository _recommendationRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetRecommendationByIdQueryHandler(
        IRecommendationRepository recommendationRepository,
        ICurrentUserService currentUserService)
    {
        _recommendationRepository = recommendationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<RecommendationDto> Handle(
        GetRecommendationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var recommendation = await _recommendationRepository.GetByIdWithTraceAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Recommendation.Recommendation), request.Id);

        if (recommendation.UserId != userId)
            throw new DomainException("Recommendation does not belong to the current user.");

        return recommendation.ToDto();
    }
}

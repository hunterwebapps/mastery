using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Recommendations.Commands.SnoozeRecommendation;

public sealed class SnoozeRecommendationCommandHandler : ICommandHandler<SnoozeRecommendationCommand>
{
    private readonly IRecommendationRepository _recommendationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public SnoozeRecommendationCommandHandler(
        IRecommendationRepository recommendationRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _recommendationRepository = recommendationRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SnoozeRecommendationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var recommendation = await _recommendationRepository.GetByIdAsync(request.RecommendationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Recommendation.Recommendation), request.RecommendationId);

        if (recommendation.UserId != userId)
            throw new DomainException("Recommendation does not belong to the current user.");

        recommendation.Snooze();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Learning.Services;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Recommendations.Commands.DismissRecommendation;

public sealed class DismissRecommendationCommandHandler : ICommandHandler<DismissRecommendationCommand>
{
    private readonly IRecommendationRepository _recommendationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILearningEngineService _learningEngine;
    private readonly IUserContextProvider _contextProvider;
    private readonly IUnitOfWork _unitOfWork;

    public DismissRecommendationCommandHandler(
        IRecommendationRepository recommendationRepository,
        ICurrentUserService currentUserService,
        ILearningEngineService learningEngine,
        IUserContextProvider contextProvider,
        IUnitOfWork unitOfWork)
    {
        _recommendationRepository = recommendationRepository;
        _currentUserService = currentUserService;
        _learningEngine = learningEngine;
        _contextProvider = contextProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DismissRecommendationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var recommendation = await _recommendationRepository.GetByIdAsync(request.RecommendationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Recommendation.Recommendation), request.RecommendationId);

        if (recommendation.UserId != userId)
            throw new DomainException("Recommendation does not belong to the current user.");

        recommendation.Dismiss(request.Reason);

        // Get current context for accurate learning
        var context = await _contextProvider.GetCurrentContextAsync(userId, cancellationToken);

        // Record outcome for learning engine with accurate context
        await _learningEngine.RecordOutcomeWithContextAsync(
            recommendation,
            wasAccepted: false,
            wasCompleted: null,
            dismissReason: request.Reason,
            context,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Learning.Services;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Recommendations.Commands.SnoozeRecommendation;

public sealed class SnoozeRecommendationCommandHandler : ICommandHandler<SnoozeRecommendationCommand>
{
    private readonly IRecommendationRepository _recommendationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILearningEngineService _learningEngine;
    private readonly IUserContextProvider _contextProvider;
    private readonly IUnitOfWork _unitOfWork;

    public SnoozeRecommendationCommandHandler(
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

    public async Task Handle(SnoozeRecommendationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var recommendation = await _recommendationRepository.GetByIdAsync(request.RecommendationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Recommendation.Recommendation), request.RecommendationId);

        if (recommendation.UserId != userId)
            throw new DomainException("Recommendation does not belong to the current user.");

        recommendation.Snooze();

        // Get current context for accurate learning
        var context = await _contextProvider.GetCurrentContextAsync(userId, cancellationToken);

        // Record outcome for learning engine with accurate context (snooze = soft dismiss)
        await _learningEngine.RecordOutcomeWithContextAsync(
            recommendation,
            wasAccepted: false,
            wasCompleted: null,
            dismissReason: "Snoozed",
            context,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

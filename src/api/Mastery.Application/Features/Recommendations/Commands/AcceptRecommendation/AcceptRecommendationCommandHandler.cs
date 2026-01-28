using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mastery.Application.Features.Recommendations.Commands.AcceptRecommendation;

public sealed class AcceptRecommendationCommandHandler : ICommandHandler<AcceptRecommendationCommand>
{
    private readonly IRecommendationRepository _recommendationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRecommendationExecutor _executor;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AcceptRecommendationCommandHandler> _logger;

    public AcceptRecommendationCommandHandler(
        IRecommendationRepository recommendationRepository,
        ICurrentUserService currentUserService,
        IRecommendationExecutor executor,
        IUnitOfWork unitOfWork,
        ILogger<AcceptRecommendationCommandHandler> logger)
    {
        _recommendationRepository = recommendationRepository;
        _currentUserService = currentUserService;
        _executor = executor;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(AcceptRecommendationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var recommendation = await _recommendationRepository.GetByIdAsync(request.RecommendationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Recommendation.Recommendation), request.RecommendationId);

        if (recommendation.UserId != userId)
            throw new DomainException("Recommendation does not belong to the current user.");

        recommendation.Accept();

        // Auto-execute: dispatch the appropriate command based on ActionPayload
        if (!string.IsNullOrEmpty(recommendation.ActionPayload))
        {
            var entityId = await _executor.ExecuteAsync(recommendation, cancellationToken);
            if (entityId is not null)
            {
                recommendation.MarkExecuted();
                _logger.LogInformation(
                    "Recommendation {RecommendationId} auto-executed: created/updated entity {EntityId}",
                    recommendation.Id, entityId);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

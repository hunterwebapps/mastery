using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Recommendations.Models;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mastery.Application.Features.Recommendations.Commands.AcceptRecommendation;

public sealed class AcceptRecommendationCommandHandler : ICommandHandler<AcceptRecommendationCommand, ExecutionResult>
{
    private readonly IRecommendationRepository _recommendationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILlmExecutor _llmExecutor;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AcceptRecommendationCommandHandler> _logger;

    public AcceptRecommendationCommandHandler(
        IRecommendationRepository recommendationRepository,
        ICurrentUserService currentUserService,
        ILlmExecutor llmExecutor,
        IUnitOfWork unitOfWork,
        ILogger<AcceptRecommendationCommandHandler> logger)
    {
        _recommendationRepository = recommendationRepository;
        _currentUserService = currentUserService;
        _llmExecutor = llmExecutor;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ExecutionResult> Handle(AcceptRecommendationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var recommendation = await _recommendationRepository.GetByIdAsync(request.RecommendationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Recommendation.Recommendation), request.RecommendationId);

        if (recommendation.UserId != userId)
            throw new DomainException("Recommendation does not belong to the current user.");

        recommendation.Accept();

        // Non-executable action kinds (reflection prompts, etc.)
        if (recommendation.ActionKind is RecommendationActionKind.ReflectPrompt
            or RecommendationActionKind.LearnPrompt)
        {
            _logger.LogInformation(
                "Recommendation {Id} accepted (non-executable {ActionKind})",
                recommendation.Id, recommendation.ActionKind);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ExecutionResult.NonExecutable();
        }

        // Execute via LLM tool calling (handles all action kinds)
        var result = await _llmExecutor.ExecuteAsync(recommendation, cancellationToken);

        if (result.Success)
        {
            recommendation.MarkExecuted();
            _logger.LogInformation(
                "Recommendation {Id} executed via LLM: {ActionKind} {EntityKind} {EntityId}",
                recommendation.Id, recommendation.ActionKind, result.EntityKind, result.EntityId);
        }
        else
        {
            _logger.LogWarning(
                "Recommendation {Id} execution failed: {Error}",
                recommendation.Id, result.ErrorMessage);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }
}

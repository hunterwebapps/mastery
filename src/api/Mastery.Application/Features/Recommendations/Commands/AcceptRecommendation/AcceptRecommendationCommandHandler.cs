using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Learning.Services;
using Mastery.Application.Features.Recommendations.Models;
using Mastery.Domain.Entities.Recommendation;
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
    private readonly ILearningEngineService _learningEngine;
    private readonly IUserContextProvider _contextProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AcceptRecommendationCommandHandler> _logger;

    public AcceptRecommendationCommandHandler(
        IRecommendationRepository recommendationRepository,
        ICurrentUserService currentUserService,
        ILlmExecutor llmExecutor,
        ILearningEngineService learningEngine,
        IUserContextProvider contextProvider,
        IUnitOfWork unitOfWork,
        ILogger<AcceptRecommendationCommandHandler> logger)
    {
        _recommendationRepository = recommendationRepository;
        _currentUserService = currentUserService;
        _llmExecutor = llmExecutor;
        _learningEngine = learningEngine;
        _contextProvider = contextProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ExecutionResult> Handle(AcceptRecommendationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var recommendation = await _recommendationRepository.GetByIdWithTraceAsync(request.RecommendationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Recommendation.Recommendation), request.RecommendationId);

        if (recommendation.UserId != userId)
            throw new DomainException("Recommendation does not belong to the current user.");

        recommendation.Accept();

        // Get current context for accurate learning
        var context = await _contextProvider.GetCurrentContextAsync(userId, cancellationToken);

        // Non-executable action kinds (reflection prompts, etc.)
        if (recommendation.ActionKind is RecommendationActionKind.ReflectPrompt
            or RecommendationActionKind.LearnPrompt)
        {
            _logger.LogInformation(
                "Recommendation {Id} accepted (non-executable {ActionKind})",
                recommendation.Id, recommendation.ActionKind);

            // Record outcome for learning engine with accurate context
            await _learningEngine.RecordOutcomeWithContextAsync(
                recommendation,
                wasAccepted: true,
                wasCompleted: true,
                dismissReason: null,
                context,
                ct: cancellationToken);

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

        // Persist AgentRun for LLM cost tracking
        if (result.LlmCall is not null && recommendation.Trace is not null)
        {
            var userIdGuid = Guid.TryParse(userId, out var uid) ? uid : Guid.Empty;
            var llmCall = result.LlmCall;

            var agentRun = llmCall.ErrorType is null
                ? AgentRun.CreateSuccessful(
                    recommendationTraceId: recommendation.Trace.Id,
                    stage: llmCall.Stage,
                    model: llmCall.Model,
                    inputTokens: llmCall.InputTokens,
                    outputTokens: llmCall.OutputTokens,
                    latencyMs: llmCall.LatencyMs,
                    startedAt: llmCall.StartedAt,
                    completedAt: llmCall.CompletedAt,
                    retryCount: 0,
                    userId: userIdGuid,
                    cachedInputTokens: llmCall.CachedInputTokens,
                    reasoningTokens: llmCall.ReasoningTokens,
                    systemFingerprint: llmCall.SystemFingerprint,
                    requestId: llmCall.RequestId,
                    provider: llmCall.Provider)
                : AgentRun.CreateFailed(
                    recommendationTraceId: recommendation.Trace.Id,
                    stage: llmCall.Stage,
                    model: llmCall.Model,
                    inputTokens: llmCall.InputTokens,
                    latencyMs: llmCall.LatencyMs,
                    errorType: llmCall.ErrorType,
                    errorMessage: llmCall.ErrorMessage,
                    startedAt: llmCall.StartedAt,
                    completedAt: llmCall.CompletedAt,
                    retryCount: 0,
                    userId: userIdGuid,
                    cachedInputTokens: llmCall.CachedInputTokens,
                    systemFingerprint: llmCall.SystemFingerprint,
                    requestId: llmCall.RequestId,
                    provider: llmCall.Provider);

            await _recommendationRepository.AddAgentRunsAsync([agentRun], cancellationToken);
        }

        // Determine completion tracking strategy:
        // - For trackable targets (Task, Habit), defer completion tracking to when the entity is completed
        // - For non-trackable targets or execution failures, record immediately
        var isTrackableTarget = recommendation.Target.Kind is
            RecommendationTargetKind.Task or
            RecommendationTargetKind.Habit or
            RecommendationTargetKind.HabitOccurrence;

        bool? wasCompleted = isTrackableTarget && result.Success
            ? null  // Will be updated later when task/habit is completed
            : result.Success;

        // Record outcome for learning engine with accurate context
        await _learningEngine.RecordOutcomeWithContextAsync(
            recommendation,
            wasAccepted: true,
            wasCompleted,
            dismissReason: null,
            context,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }
}

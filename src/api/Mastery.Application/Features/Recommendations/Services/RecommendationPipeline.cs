using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Recommendations.Models;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mastery.Application.Features.Recommendations.Services;

/// <summary>
/// Orchestrates the full recommendation pipeline using tiered assessment:
/// 1. Assemble state → 2. Create synthetic signal → 3. Run TieredAssessmentEngine → 4. Expire stale
/// </summary>
public sealed class RecommendationPipeline(
    IUserStateAssembler _stateAssembler,
    ITieredAssessmentEngine _tieredEngine,
    IRecommendationRepository _recommendationRepository,
    IUnitOfWork _unitOfWork,
    IDateTimeProvider _dateTimeProvider,
    ILogger<RecommendationPipeline> _logger)
    : IRecommendationPipeline
{
    public async Task<IReadOnlyList<RecommendationSummaryDto>> ExecuteAsync(
        string userId,
        RecommendationContext context,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Assemble user state
        _logger.LogInformation("Pipeline: assembling state for user {UserId}, context {Context}", userId, context);
        var state = await _stateAssembler.AssembleAsync(userId, cancellationToken);

        // Step 2: Create a synthetic signal for API-triggered recommendations
        var syntheticSignal = SignalEntry.Create(
            userId: userId,
            eventType: $"ApiTriggered_{context}",
            priority: SignalPriority.Standard,
            windowType: context switch
            {
                RecommendationContext.MorningCheckIn => ProcessingWindowType.MorningWindow,
                RecommendationContext.EveningCheckIn => ProcessingWindowType.EveningWindow,
                RecommendationContext.WeeklyReview => ProcessingWindowType.WeeklyReview,
                _ => ProcessingWindowType.Immediate
            },
            createdAt: _dateTimeProvider.UtcNow);

        // Step 3: Run through tiered assessment (Tier 0 → Tier 1 → Tier 2)
        _logger.LogInformation("Pipeline: running tiered assessment");
        var outcome = await _tieredEngine.AssessAsync(state, [syntheticSignal], cancellationToken);

        if (outcome.GeneratedRecommendations.Count == 0)
        {
            _logger.LogInformation("Pipeline: no recommendations generated for user {UserId}", userId);

            // Still expire stale recommendations even if none generated
            var expireCutoff = _dateTimeProvider.UtcNow.AddHours(-24);
            await _recommendationRepository.ExpirePendingBeforeAsync(userId, expireCutoff, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return [];
        }

        // Recommendations are already persisted by TieredAssessmentEngine
        // Just expire stale ones
        var cutoff = _dateTimeProvider.UtcNow.AddHours(-24);
        await _recommendationRepository.ExpirePendingBeforeAsync(userId, cutoff, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Pipeline: {Count} recommendations via {Tier} for user {UserId}",
            outcome.GeneratedRecommendations.Count, outcome.FinalTier, userId);

        return outcome.GeneratedRecommendations.Select(r => r.ToSummaryDto()).ToList();
    }
}

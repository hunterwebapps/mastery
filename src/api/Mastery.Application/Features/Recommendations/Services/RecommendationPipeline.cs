using System.Text.Json;
using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Recommendations.Models;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mastery.Application.Features.Recommendations.Services;

/// <summary>
/// Orchestrates the full recommendation pipeline:
/// 1. Assemble state → 2. LLM orchestrate (Assessment → Strategy → Generation) → 3. Persist → 4. Expire stale
/// </summary>
public sealed class RecommendationPipeline(
    IUserStateAssembler _stateAssembler,
    IRecommendationOrchestrator _orchestrator,
    IRecommendationRepository _recommendationRepository,
    IUnitOfWork _unitOfWork,
    IDateTimeProvider _dateTimeProvider,
    ILogger<RecommendationPipeline> _logger)
    : IRecommendationPipeline
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public async Task<IReadOnlyList<RecommendationSummaryDto>> ExecuteAsync(
        string userId,
        RecommendationContext context,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Assemble user state
        _logger.LogInformation("Pipeline: assembling state for user {UserId}, context {Context}", userId, context);
        var state = await _stateAssembler.AssembleAsync(userId, cancellationToken);

        // Step 2: LLM orchestrate (Assessment → Strategy → Generation)
        _logger.LogInformation("Pipeline: running LLM orchestration");
        var orchestrationResult = await _orchestrator.OrchestrateAsync(state, context, cancellationToken);

        if (orchestrationResult.SelectedCandidates.Count == 0)
        {
            _logger.LogInformation("Pipeline: no recommendations generated for user {UserId}", userId);

            // Still expire stale recommendations even if none generated
            var expireCutoff = _dateTimeProvider.UtcNow.AddHours(-24);
            await _recommendationRepository.ExpirePendingBeforeAsync(userId, expireCutoff, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return [];
        }

        // Step 3: Persist recommendations with traces
        var recommendations = new List<Recommendation>();
        var expiresAt = _dateTimeProvider.UtcNow.AddHours(24);

        var stateJson = JsonSerializer.Serialize(
            new { userId, goalsCount = state.Goals.Count, habitsCount = state.Habits.Count, tasksCount = state.Tasks.Count },
            JsonOptions);

        foreach (var candidate in orchestrationResult.SelectedCandidates)
        {
            var rec = Recommendation.Create(
                userId: userId,
                type: candidate.Type,
                context: context,
                target: candidate.Target,
                actionKind: candidate.ActionKind,
                title: candidate.Title,
                rationale: candidate.Rationale,
                score: candidate.Score,
                actionPayload: candidate.ActionPayload,
                actionSummary: candidate.ActionSummary,
                expiresAt: expiresAt,
                signalIds: candidate.ContributingSignalIds);

            var trace = RecommendationTrace.Create(
                recommendationId: rec.Id,
                stateSnapshotJson: stateJson,
                signalsSummaryJson: "[]",
                candidateListJson: "[]",
                selectionMethod: orchestrationResult.SelectionMethod,
                promptVersion: orchestrationResult.PromptVersion,
                modelVersion: orchestrationResult.ModelVersion,
                rawLlmResponse: orchestrationResult.RawResponse);

            rec.AttachTrace(trace);
            await _recommendationRepository.AddAsync(rec, cancellationToken);
            recommendations.Add(rec);
        }

        // Step 4: Expire stale pending recommendations (>24h old)
        var cutoff = _dateTimeProvider.UtcNow.AddHours(-24);
        await _recommendationRepository.ExpirePendingBeforeAsync(userId, cutoff, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Pipeline: persisted {Count} recommendations for user {UserId}", recommendations.Count, userId);

        return recommendations.Select(r => r.ToSummaryDto()).ToList();
    }
}

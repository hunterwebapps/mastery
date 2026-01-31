using System.Diagnostics;
using Mastery.Application.Common.Interfaces;
using Mastery.Application.Common.Models;
using Mastery.Domain.Diagnostics;
using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;
using Mastery.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Mastery.Application.Features.Recommendations.Services;

/// <summary>
/// Orchestrates the tiered assessment pipeline: Tier 0 → Tier 1 → Tier 2.
/// Each tier can produce recommendations; escalation is determined by scoring.
/// </summary>
public sealed class TieredAssessmentEngine(
    IDeterministicRulesEngine _tier0Engine,
    IQuickAssessmentService _tier1Service,
    IRecommendationOrchestrator _tier2Orchestrator,
    IStateDeltaCalculator _deltaCalculator,
    IDateTimeProvider _dateTimeProvider,
    ILogger<TieredAssessmentEngine> _logger)
    : ITieredAssessmentEngine
{
    public async Task<TieredAssessmentOutcome> AssessAsync(
        UserStateSnapshot state,
        IReadOnlyList<SignalEntry> signals,
        CancellationToken ct = default)
    {
        var startedAt = _dateTimeProvider.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting tiered assessment for user {UserId} with {SignalCount} signals",
            state.UserId,
            signals.Count);

        var recommendations = new List<Recommendation>();
        QuickAssessmentResult? tier1Result = null;
        var tier2Executed = false;
        var tier2LlmCalls = 0;

        // ═══════════════════════════════════════════════════════════════════
        // TIER 0: Deterministic Rules
        // ═══════════════════════════════════════════════════════════════════
        var tier0Result = await _tier0Engine.EvaluateAsync(state, signals, ct);

        _logger.LogDebug(
            "Tier 0 complete: {Triggered}/{Total} rules triggered, {DirectRecs} direct recommendations",
            tier0Result.TriggeredRules.Count,
            tier0Result.AllResults.Count,
            tier0Result.DirectRecommendations.Count);

        // Convert Tier 0 direct recommendations to Recommendation entities
        var tier0Recommendations = CreateRecommendationsFromCandidates(
            state.UserId,
            tier0Result.DirectRecommendations,
            signals);

        recommendations.AddRange(tier0Recommendations);

        // Decide whether to escalate to Tier 1
        var shouldEscalateToTier1 = tier0Result.ShouldEscalateToTier1 ||
                                     tier0Result.TriggeredRules.Any(r => r.RequiresEscalation) ||
                                     signals.Any(s => s.Priority == SignalPriority.Urgent);

        if (!shouldEscalateToTier1 && tier0Result.DirectRecommendations.Count > 0)
        {
            _logger.LogInformation(
                "Tier 0 sufficient for user {UserId}: {RecCount} recommendations generated",
                state.UserId,
                tier0Recommendations.Count);

            return BuildOutcome(
                state,
                signals,
                tier0Result,
                null,
                false,
                recommendations,
                0,
                startedAt,
                stopwatch);
        }

        // ═══════════════════════════════════════════════════════════════════
        // TIER 1: Quick Assessment (Vector Search + State Delta)
        // ═══════════════════════════════════════════════════════════════════
        tier1Result = await _tier1Service.AssessAsync(state, signals, tier0Result, ct);

        _logger.LogDebug(
            "Tier 1 complete: combined score={Score:F2}, escalate={Escalate}",
            tier1Result.CombinedScore,
            tier1Result.ShouldEscalateToTier2);

        // Decide whether to escalate to Tier 2
        if (!tier1Result.ShouldEscalateToTier2)
        {
            _logger.LogInformation(
                "Tier 1 assessment complete for user {UserId}: score={Score:F2}, no Tier 2 needed",
                state.UserId,
                tier1Result.CombinedScore);

            // Tier 0 recommendations are sufficient
            return BuildOutcome(
                state,
                signals,
                tier0Result,
                tier1Result,
                false,
                recommendations,
                0,
                startedAt,
                stopwatch);
        }

        // ═══════════════════════════════════════════════════════════════════
        // TIER 2: Full LLM Pipeline
        // ═══════════════════════════════════════════════════════════════════
        _logger.LogInformation(
            "Escalating to Tier 2 for user {UserId}: {Reason}",
            state.UserId,
            tier1Result.EscalationReason);

        tier2Executed = true;

        try
        {
            // Determine context based on signals
            var context = DetermineRecommendationContext(signals);

            var orchestrationResult = await _tier2Orchestrator.OrchestrateAsync(
                state,
                context,
                ct);

            tier2LlmCalls = 1; // At minimum, one orchestrator call

            // Convert candidates to Recommendation entities
            var tier2Recommendations = CreateRecommendationsFromOrchestration(
                state.UserId,
                orchestrationResult,
                context);

            recommendations.AddRange(tier2Recommendations);

            _logger.LogInformation(
                "Tier 2 complete for user {UserId}: {RecCount} additional recommendations via {Method}",
                state.UserId,
                tier2Recommendations.Count,
                orchestrationResult.SelectionMethod);

            // Record baseline for future delta calculations
            await _deltaCalculator.RecordBaselineAsync(state.UserId, state, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Tier 2 failed for user {UserId}, falling back to Tier 0/1 recommendations",
                state.UserId);

            // Don't fail completely - return what we have from earlier tiers
        }

        return BuildOutcome(
            state,
            signals,
            tier0Result,
            tier1Result,
            tier2Executed,
            recommendations,
            tier2LlmCalls,
            startedAt,
            stopwatch);
    }

    private IReadOnlyList<Recommendation> CreateRecommendationsFromCandidates(
        string userId,
        IReadOnlyList<DirectRecommendationCandidate> candidates,
        IReadOnlyList<SignalEntry> signals)
    {
        var recommendations = new List<Recommendation>();

        foreach (var candidate in candidates)
        {
            var target = RecommendationTarget.Create(
                candidate.TargetKind,
                candidate.TargetEntityId,
                candidate.TargetEntityTitle);

            var recommendation = Recommendation.Create(
                userId: userId,
                type: candidate.Type,
                context: candidate.Context,
                target: target,
                actionKind: candidate.ActionKind,
                title: candidate.Title,
                rationale: candidate.Rationale,
                score: candidate.Score,
                actionPayload: candidate.ActionPayload,
                actionSummary: candidate.ActionSummary,
                expiresAt: _dateTimeProvider.UtcNow.AddHours(24));

            recommendations.Add(recommendation);
        }

        return recommendations;
    }

    private IReadOnlyList<Recommendation> CreateRecommendationsFromOrchestration(
        string userId,
        RecommendationOrchestrationResult orchestrationResult,
        RecommendationContext context)
    {
        var recommendations = new List<Recommendation>();

        foreach (var candidate in orchestrationResult.SelectedCandidates)
        {
            var recommendation = Recommendation.Create(
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
                expiresAt: _dateTimeProvider.UtcNow.AddHours(24),
                signalIds: candidate.ContributingSignalIds);

            recommendations.Add(recommendation);
        }

        return recommendations;
    }

    private static RecommendationContext DetermineRecommendationContext(IReadOnlyList<SignalEntry> signals)
    {
        // Determine context based on signal types and window
        var hasCheckInSignal = signals.Any(s =>
            s.EventType.Contains("CheckIn", StringComparison.OrdinalIgnoreCase));

        if (hasCheckInSignal)
        {
            var morningCheckIn = signals.Any(s =>
                s.EventType.Contains("Morning", StringComparison.OrdinalIgnoreCase) ||
                s.WindowType == ProcessingWindowType.MorningWindow);

            return morningCheckIn
                ? RecommendationContext.MorningCheckIn
                : RecommendationContext.EveningCheckIn;
        }

        var hasWeeklySignal = signals.Any(s =>
            s.WindowType == ProcessingWindowType.WeeklyReview ||
            s.EventType.Contains("Weekly", StringComparison.OrdinalIgnoreCase));

        if (hasWeeklySignal)
        {
            return RecommendationContext.WeeklyReview;
        }

        var hasUrgentSignal = signals.Any(s => s.Priority == SignalPriority.Urgent);

        return hasUrgentSignal
            ? RecommendationContext.DriftAlert
            : RecommendationContext.ProactiveCheck;
    }

    private TieredAssessmentOutcome BuildOutcome(
        UserStateSnapshot state,
        IReadOnlyList<SignalEntry> signals,
        RuleEvaluationResult tier0Result,
        QuickAssessmentResult? tier1Result,
        bool tier2Executed,
        List<Recommendation> recommendations,
        int tier2LlmCalls,
        DateTime startedAt,
        Stopwatch stopwatch)
    {
        stopwatch.Stop();

        var statistics = new TieredAssessmentStatistics(
            Tier0RulesEvaluated: tier0Result.AllResults.Count,
            Tier0RulesTriggered: tier0Result.TriggeredRules.Count,
            Tier0DirectRecommendations: tier0Result.DirectRecommendations.Count,
            Tier1CombinedScore: tier1Result?.CombinedScore,
            Tier1RelevantContextItems: tier1Result?.RelevantContext.Count ?? 0,
            Tier2LlmCallsMade: tier2LlmCalls,
            DurationMs: stopwatch.ElapsedMilliseconds);

        return new TieredAssessmentOutcome(
            UserId: state.UserId,
            ProcessedSignals: signals,
            Tier0Result: tier0Result,
            Tier1Result: tier1Result,
            Tier2Executed: tier2Executed,
            GeneratedRecommendations: recommendations,
            Statistics: statistics,
            StartedAt: startedAt,
            CompletedAt: _dateTimeProvider.UtcNow);
    }
}

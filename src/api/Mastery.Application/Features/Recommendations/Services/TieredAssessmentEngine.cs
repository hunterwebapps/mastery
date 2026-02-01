using System.Diagnostics;
using Mastery.Application.Common;
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
    IRecommendationPolicyEnforcer _policyEnforcer,
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
        var agentRuns = new List<AgentRun>();
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
            state,
            tier0Result.DirectRecommendations,
            tier0Result.AllResults,
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

            // Apply policy enforcement to Tier 0 recommendations
            var tier0Context = DetermineRecommendationContext(signals);
            var tier0PolicyResult = await _policyEnforcer.EnforceAsync(
                recommendations,
                state,
                tier0Context,
                ct);

            return BuildOutcome(
                state,
                signals,
                tier0Result,
                null,
                false,
                tier0PolicyResult.ApprovedRecommendations.ToList(),
                0,
                startedAt,
                stopwatch,
                tier0PolicyResult,
                []);
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

            // Apply policy enforcement to Tier 0 recommendations
            var tier1Context = DetermineRecommendationContext(signals);
            var tier1PolicyResult = await _policyEnforcer.EnforceAsync(
                recommendations,
                state,
                tier1Context,
                ct);

            return BuildOutcome(
                state,
                signals,
                tier0Result,
                tier1Result,
                false,
                tier1PolicyResult.ApprovedRecommendations.ToList(),
                0,
                startedAt,
                stopwatch,
                tier1PolicyResult,
                []);
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
                tier0Result.DirectRecommendations,
                ct);

            tier2LlmCalls = orchestrationResult.LlmCalls?.Count ?? 0;

            // Convert candidates to Recommendation entities
            var tier2Recommendations = CreateRecommendationsFromOrchestration(
                state,
                signals,
                orchestrationResult,
                context);

            recommendations.AddRange(tier2Recommendations);

            // Create AgentRun entities from LLM call records
            if (tier2LlmCalls > 0 && tier2Recommendations.Count > 0)
            {
                // Link all agent runs to the first recommendation's trace
                var firstTraceId = tier2Recommendations[0].Trace?.Id ?? Guid.Empty;
                var userIdGuid = Guid.TryParse(state.UserId, out var uid) ? uid : Guid.Empty;

                foreach (var llmCall in orchestrationResult.LlmCalls!)
                {
                    var agentRun = llmCall.ErrorType is null
                        ? AgentRun.CreateSuccessful(
                            recommendationTraceId: firstTraceId,
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
                            recommendationTraceId: firstTraceId,
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

                    agentRuns.Add(agentRun);
                }
            }

            _logger.LogInformation(
                "Tier 2 complete for user {UserId}: {RecCount} additional recommendations via {Method}, {LlmCalls} LLM calls",
                state.UserId,
                tier2Recommendations.Count,
                orchestrationResult.SelectionMethod,
                tier2LlmCalls);

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

        // ═══════════════════════════════════════════════════════════════════
        // POLICY ENFORCEMENT: Validate all recommendations before returning
        // ═══════════════════════════════════════════════════════════════════
        var finalContext = tier2Executed
            ? DetermineRecommendationContext(signals)
            : RecommendationContext.ProactiveCheck;

        var policyResult = await _policyEnforcer.EnforceAsync(
            recommendations,
            state,
            finalContext,
            ct);

        if (policyResult.HadAdjustments)
        {
            _logger.LogInformation(
                "Policy enforcement adjusted recommendations: {Approved} approved, {Rejected} rejected",
                policyResult.ApprovedRecommendations.Count,
                policyResult.RejectedRecommendations.Count);
        }

        return BuildOutcome(
            state,
            signals,
            tier0Result,
            tier1Result,
            tier2Executed,
            policyResult.ApprovedRecommendations.ToList(),
            tier2LlmCalls,
            startedAt,
            stopwatch,
            policyResult,
            agentRuns);
    }

    private IReadOnlyList<Recommendation> CreateRecommendationsFromCandidates(
        UserStateSnapshot state,
        IReadOnlyList<DirectRecommendationCandidate> candidates,
        IReadOnlyList<RuleResult> allRuleResults,
        IReadOnlyList<SignalEntry> signals)
    {
        var recommendations = new List<Recommendation>();

        // Prepare trace data once for all candidates
        var stateSnapshotJson = JsonCompressionHelper.SerializeCompressed(state);
        var signalsSummaryJson = JsonCompressionHelper.Serialize(
            signals.Select(s => new { s.EventType, s.Priority, s.WindowType, s.TargetEntityType }));
        var candidateListJson = JsonCompressionHelper.Serialize(
            allRuleResults.Where(r => r.Triggered).Select(r => new
            {
                r.RuleName,
                r.Severity,
                r.DirectRecommendation?.Score,
                r.DirectRecommendation?.Title,
                r.Evidence
            }));

        foreach (var candidate in candidates)
        {
            var target = RecommendationTarget.Create(
                candidate.TargetKind,
                candidate.TargetEntityId,
                candidate.TargetEntityTitle);

            var recommendation = Recommendation.Create(
                userId: state.UserId,
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

            // Create and attach trace for explainability
            var trace = RecommendationTrace.Create(
                recommendationId: recommendation.Id,
                stateSnapshotJson: stateSnapshotJson,
                signalsSummaryJson: signalsSummaryJson,
                candidateListJson: candidateListJson,
                selectionMethod: "Tier0-Rules");

            recommendation.AttachTrace(trace);
            recommendations.Add(recommendation);
        }

        return recommendations;
    }

    private IReadOnlyList<Recommendation> CreateRecommendationsFromOrchestration(
        UserStateSnapshot state,
        IReadOnlyList<SignalEntry> signals,
        RecommendationOrchestrationResult orchestrationResult,
        RecommendationContext context)
    {
        var recommendations = new List<Recommendation>();

        // Prepare trace data once for all candidates
        var stateSnapshotJson = JsonCompressionHelper.SerializeCompressed(state);
        var signalsSummaryJson = JsonCompressionHelper.Serialize(
            signals.Select(s => new { s.EventType, s.Priority, s.WindowType, s.TargetEntityType }));
        var candidateListJson = JsonCompressionHelper.Serialize(
            orchestrationResult.SelectedCandidates.Select(c => new
            {
                c.Type,
                c.Title,
                c.Score,
                TargetKind = c.Target.Kind,
                TargetEntityId = c.Target.EntityId
            }));

        foreach (var candidate in orchestrationResult.SelectedCandidates)
        {
            var recommendation = Recommendation.Create(
                userId: state.UserId,
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

            // Create and attach trace for explainability
            var trace = RecommendationTrace.Create(
                recommendationId: recommendation.Id,
                stateSnapshotJson: stateSnapshotJson,
                signalsSummaryJson: signalsSummaryJson,
                candidateListJson: candidateListJson,
                selectionMethod: orchestrationResult.SelectionMethod,
                promptVersion: orchestrationResult.PromptVersion,
                modelVersion: orchestrationResult.ModelVersion,
                rawLlmResponse: orchestrationResult.RawResponse);

            recommendation.AttachTrace(trace);
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
        Stopwatch stopwatch,
        PolicyEnforcementResult policyResult,
        IReadOnlyList<AgentRun> agentRuns)
    {
        stopwatch.Stop();

        var statistics = new TieredAssessmentStatistics(
            Tier0RulesEvaluated: tier0Result.AllResults.Count,
            Tier0RulesTriggered: tier0Result.TriggeredRules.Count,
            Tier0DirectRecommendations: tier0Result.DirectRecommendations.Count,
            Tier1CombinedScore: tier1Result?.CombinedScore,
            Tier1RelevantContextItems: tier1Result?.RelevantContext.Count ?? 0,
            Tier2LlmCallsMade: tier2LlmCalls,
            DurationMs: stopwatch.ElapsedMilliseconds,
            PolicyRejectionsCount: policyResult.RejectedRecommendations.Count,
            PolicyViolationsCount: policyResult.Violations.Count);

        return new TieredAssessmentOutcome(
            UserId: state.UserId,
            ProcessedSignals: signals,
            Tier0Result: tier0Result,
            Tier1Result: tier1Result,
            Tier2Executed: tier2Executed,
            GeneratedRecommendations: recommendations,
            Statistics: statistics,
            StartedAt: startedAt,
            CompletedAt: _dateTimeProvider.UtcNow,
            PolicyEnforcementResult: policyResult,
            AgentRuns: agentRuns);
    }
}

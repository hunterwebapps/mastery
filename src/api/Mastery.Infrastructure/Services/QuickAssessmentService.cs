using System.Text;
using Mastery.Application.Common.Interfaces;
using Mastery.Application.Common.Models;
using Mastery.Domain.Diagnostics;
using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Entities.Signal;
using Mastery.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Mastery.Infrastructure.Services;

/// <summary>
/// Tier 1 quick assessment service that uses state delta and vector search
/// to efficiently determine whether to escalate to Tier 2 (full LLM pipeline).
/// </summary>
public sealed class QuickAssessmentService(
    IStateDeltaCalculator _deltaCalculator,
    IVectorStore _vectorStore,
    IEmbeddingService _embeddingService,
    IDateTimeProvider _dateTimeProvider,
    ILogger<QuickAssessmentService> _logger)
    : IQuickAssessmentService
{
    private const int MaxRelevantContextItems = 10;
    private const double MinSimilarityThreshold = 0.5;

    public async Task<QuickAssessmentResult> AssessAsync(
        UserStateSnapshot state,
        IReadOnlyList<SignalEntry> signals,
        RuleEvaluationResult tier0Result,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Starting Tier 1 assessment for user {UserId} with {SignalCount} signals",
            state.UserId,
            signals.Count);

        // 1. Calculate state delta
        var deltaSummary = await _deltaCalculator.CalculateAsync(
            state.UserId,
            state,
            signals,
            ct);

        // 2. Calculate urgency score from signals and Tier 0 results
        var urgencyScore = CalculateUrgencyScore(signals, tier0Result);

        // 3. Find relevant context via vector search
        var relevantContext = await FindRelevantContextAsync(
            state.UserId,
            signals,
            tier0Result,
            ct);

        // 4. Calculate relevance score from vector search results
        var relevanceScore = CalculateRelevanceScore(relevantContext);

        // 5. Calculate combined score
        var combinedScore = QuickAssessmentResult.CalculateCombinedScore(
            relevanceScore,
            deltaSummary.OverallDeltaScore,
            urgencyScore);

        // 6. Determine escalation
        var (shouldEscalate, escalationReason) = DetermineEscalation(
            combinedScore,
            tier0Result,
            deltaSummary,
            relevanceScore,
            urgencyScore);

        // 7. Build signal summary
        var signalSummary = signals.Select(s => new SignalSummaryItem(
            s.Id,
            s.EventType,
            s.Priority,
            s.WindowType,
            s.TargetEntityType,
            s.TargetEntityId,
            s.CreatedAt)).ToList();

        var result = new QuickAssessmentResult(
            UserId: state.UserId,
            RelevanceScore: relevanceScore,
            DeltaScore: deltaSummary.OverallDeltaScore,
            UrgencyScore: urgencyScore,
            CombinedScore: combinedScore,
            ShouldEscalateToTier2: shouldEscalate,
            EscalationReason: escalationReason,
            RelevantContext: relevantContext,
            DeltaSummary: deltaSummary,
            SignalSummary: signalSummary,
            AssessedAt: _dateTimeProvider.UtcNow);

        _logger.LogInformation(
            "Tier 1 assessment for user {UserId}: relevance={Relevance:F2}, delta={Delta:F2}, urgency={Urgency:F2}, combined={Combined:F2}, escalate={Escalate}",
            state.UserId,
            relevanceScore,
            deltaSummary.OverallDeltaScore,
            urgencyScore,
            combinedScore,
            shouldEscalate);

        return result;
    }

    private decimal CalculateUrgencyScore(
        IReadOnlyList<SignalEntry> signals,
        RuleEvaluationResult tier0Result)
    {
        var score = 0m;

        // Factor 1: Signal priorities (0-0.4)
        var urgentCount = signals.Count(s => s.Priority == SignalPriority.Urgent);
        var windowAlignedCount = signals.Count(s => s.Priority == SignalPriority.WindowAligned);
        score += Math.Min(urgentCount * 0.2m, 0.4m);
        score += Math.Min(windowAlignedCount * 0.05m, 0.1m);

        // Factor 2: Tier 0 severity (0-0.4)
        if (tier0Result.MaxSeverity.HasValue)
        {
            score += tier0Result.MaxSeverity.Value switch
            {
                RuleSeverity.Critical => 0.4m,
                RuleSeverity.High => 0.3m,
                RuleSeverity.Medium => 0.15m,
                RuleSeverity.Low => 0.05m,
                _ => 0m
            };
        }

        // Factor 3: Number of triggered rules (0-0.2)
        score += Math.Min(tier0Result.TriggeredRules.Count * 0.05m, 0.2m);

        return Math.Min(score, 1.0m);
    }

    private async Task<IReadOnlyList<RelevantContextItem>> FindRelevantContextAsync(
        string userId,
        IReadOnlyList<SignalEntry> signals,
        RuleEvaluationResult tier0Result,
        CancellationToken ct)
    {
        // Build a query from signals and triggered rules
        var queryText = BuildSearchQuery(signals, tier0Result);

        if (string.IsNullOrWhiteSpace(queryText))
        {
            _logger.LogDebug("No search query generated, skipping vector search");
            return [];
        }

        try
        {
            // Generate embedding for the query
            var queryVector = await _embeddingService.GenerateEmbeddingAsync(queryText, ct);

            // Search for relevant context
            var searchResults = await _vectorStore.SearchAsync(
                userId,
                queryVector,
                topK: MaxRelevantContextItems,
                entityTypes: null, // Search all entity types
                ct: ct);

            // Filter by similarity threshold and convert to context items
            var relevantItems = searchResults
                .Where(r => r.Score >= MinSimilarityThreshold)
                .Select(r => new RelevantContextItem(
                    EntityType: r.EntityType,
                    EntityId: r.EntityId,
                    Title: r.Title,
                    Status: r.Status,
                    SimilarityScore: r.Score,
                    RelevanceReason: DetermineRelevanceReason(r, signals)))
                .ToList();

            _logger.LogDebug(
                "Vector search returned {Count} relevant items for user {UserId}",
                relevantItems.Count,
                userId);

            return relevantItems;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Vector search failed for user {UserId}, continuing without context",
                userId);
            return [];
        }
    }

    private static string BuildSearchQuery(
        IReadOnlyList<SignalEntry> signals,
        RuleEvaluationResult tier0Result)
    {
        var queryBuilder = new StringBuilder();

        // Add signal event types as context
        var eventTypes = signals
            .Select(s => s.EventType)
            .Distinct()
            .Take(5);

        foreach (var eventType in eventTypes)
        {
            queryBuilder.Append(HumanizeEventType(eventType));
            queryBuilder.Append(' ');
        }

        // Add triggered rule names
        foreach (var rule in tier0Result.TriggeredRules.Take(3))
        {
            queryBuilder.Append(rule.RuleName);
            queryBuilder.Append(' ');
        }

        // Add entity titles from direct recommendations
        foreach (var rec in tier0Result.DirectRecommendations.Take(3))
        {
            if (!string.IsNullOrEmpty(rec.TargetEntityTitle))
            {
                queryBuilder.Append(rec.TargetEntityTitle);
                queryBuilder.Append(' ');
            }
        }

        return queryBuilder.ToString().Trim();
    }

    private static string HumanizeEventType(string eventType)
    {
        // Convert "CheckInSubmitted" to "check in submitted"
        var result = new StringBuilder();
        foreach (var c in eventType)
        {
            if (char.IsUpper(c) && result.Length > 0)
            {
                result.Append(' ');
            }
            result.Append(char.ToLowerInvariant(c));
        }
        return result.ToString();
    }

    private static string? DetermineRelevanceReason(
        VectorSearchResult result,
        IReadOnlyList<SignalEntry> signals)
    {
        // Check if the result matches a signal target
        var matchingSignal = signals.FirstOrDefault(s =>
            s.TargetEntityId == result.EntityId &&
            string.Equals(s.TargetEntityType, result.EntityType, StringComparison.OrdinalIgnoreCase));

        if (matchingSignal != null)
        {
            return $"Direct target of {matchingSignal.EventType} signal";
        }

        // Check entity type relevance
        var signalEntityTypes = signals
            .Where(s => !string.IsNullOrEmpty(s.TargetEntityType))
            .Select(s => s.TargetEntityType)
            .Distinct();

        if (signalEntityTypes.Any(t => string.Equals(t, result.EntityType, StringComparison.OrdinalIgnoreCase)))
        {
            return $"Same entity type as triggered signals";
        }

        return result.Score > 0.8 ? "Highly similar content" : "Related context";
    }

    private static decimal CalculateRelevanceScore(IReadOnlyList<RelevantContextItem> context)
    {
        if (context.Count == 0)
            return 0m;

        // Average similarity of top results, weighted by position
        var weightedSum = 0m;
        var totalWeight = 0m;

        for (int i = 0; i < context.Count; i++)
        {
            var weight = 1m / (i + 1); // Decreasing weight by position
            weightedSum += (decimal)context[i].SimilarityScore * weight;
            totalWeight += weight;
        }

        var avgSimilarity = totalWeight > 0 ? weightedSum / totalWeight : 0m;

        // Scale and boost by number of results
        var countBoost = Math.Min(context.Count / 10m, 0.2m);

        return Math.Min(avgSimilarity + countBoost, 1.0m);
    }

    private static (bool ShouldEscalate, string? Reason) DetermineEscalation(
        decimal combinedScore,
        RuleEvaluationResult tier0Result,
        StateDeltaSummary deltaSummary,
        decimal relevanceScore,
        decimal urgencyScore)
    {
        // Primary check: combined score threshold
        if (combinedScore >= QuickAssessmentResult.EscalationThreshold)
        {
            return (true, $"Combined score {combinedScore:F2} exceeds threshold {QuickAssessmentResult.EscalationThreshold}");
        }

        // Secondary checks that can override the threshold

        // Tier 0 already requested escalation
        if (tier0Result.ShouldEscalateToTier1)
        {
            return (true, tier0Result.EscalationReason ?? "Tier 0 requested escalation");
        }

        // Critical severity always escalates
        if (tier0Result.MaxSeverity == RuleSeverity.Critical)
        {
            return (true, "Critical severity issue detected");
        }

        // Very high urgency with moderate delta
        if (urgencyScore > 0.7m && deltaSummary.OverallDeltaScore > 0.3m)
        {
            return (true, "High urgency with significant state changes");
        }

        // Many missed items is concerning
        if (deltaSummary.MissedItemsCount >= 3)
        {
            return (true, $"{deltaSummary.MissedItemsCount} missed items detected");
        }

        // No escalation needed
        return (false, null);
    }
}

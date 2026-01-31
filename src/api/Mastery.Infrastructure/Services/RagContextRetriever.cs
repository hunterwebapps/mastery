using System.Diagnostics;
using System.Text;
using Mastery.Application.Common.Interfaces;
using Mastery.Application.Common.Models;
using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Enums;
using Mastery.Infrastructure.Services.Prompts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mastery.Infrastructure.Services;

/// <summary>
/// Internal interface for RAG context retrieval within the LLM pipeline.
/// </summary>
internal interface IRagContextRetriever
{
    Task<RagContext?> RetrieveForAssessmentAsync(
        UserStateSnapshot state,
        RecommendationContext context,
        CancellationToken ct = default);

    Task<RagContext?> RetrieveForStrategyAsync(
        SituationalAssessment assessment,
        RecommendationContext context,
        string userId,
        CancellationToken ct = default);

    Task<RagContext?> RetrieveForGenerationAsync(
        string domain,
        SituationalAssessment assessment,
        IReadOnlyList<InterventionPlanItem> interventions,
        UserStateSnapshot state,
        CancellationToken ct = default);
}

/// <summary>
/// Retrieves semantically relevant historical context via RAG for each stage of the LLM pipeline.
/// Uses per-request embedding caching to avoid redundant embedding generation.
/// </summary>
internal sealed class RagContextRetriever(
    IVectorStore _vectorStore,
    IEmbeddingService _embeddingService,
    IOptions<RagOptions> _options,
    ILogger<RagContextRetriever> _logger)
    : IRagContextRetriever
{
    // Per-request cache for embeddings (service is Scoped)
    private readonly Dictionary<string, float[]> _embeddingCache = new();

    public async Task<RagContext?> RetrieveForAssessmentAsync(
        UserStateSnapshot state,
        RecommendationContext context,
        CancellationToken ct = default)
    {
        var options = _options.Value;

        var queryText = BuildAssessmentQuery(state, context);
        if (string.IsNullOrWhiteSpace(queryText))
        {
            _logger.LogDebug("No assessment query generated, skipping RAG");
            return null;
        }

        return await RetrieveContextAsync(
            state.UserId,
            queryText,
            RagContextStage.Assessment,
            options.Assessment,
            ct);
    }

    public async Task<RagContext?> RetrieveForStrategyAsync(
        SituationalAssessment assessment,
        RecommendationContext context,
        string userId,
        CancellationToken ct = default)
    {
        var options = _options.Value;

        var queryText = BuildStrategyQuery(assessment, context);
        if (string.IsNullOrWhiteSpace(queryText))
        {
            _logger.LogDebug("No strategy query generated, skipping RAG");
            return null;
        }

        return await RetrieveContextAsync(
            userId,
            queryText,
            RagContextStage.Strategy,
            options.Strategy,
            ct);
    }

    public async Task<RagContext?> RetrieveForGenerationAsync(
        string domain,
        SituationalAssessment assessment,
        IReadOnlyList<InterventionPlanItem> interventions,
        UserStateSnapshot state,
        CancellationToken ct = default)
    {
        var options = _options.Value;

        var queryText = BuildGenerationQuery(domain, assessment, interventions);
        if (string.IsNullOrWhiteSpace(queryText))
        {
            _logger.LogDebug("No generation query generated for domain {Domain}, skipping RAG", domain);
            return null;
        }

        return await RetrieveContextAsync(
            state.UserId,
            queryText,
            RagContextStage.Generation,
            options.Generation,
            ct);
    }

    // ─────────────────────────────────────────────────────────────────
    // Query Builders
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds search query for Assessment stage based on:
    /// - Energy trends from recent check-ins
    /// - Capacity signals
    /// - Recent blocker patterns
    /// </summary>
    private static string BuildAssessmentQuery(UserStateSnapshot state, RecommendationContext context)
    {
        var sb = new StringBuilder();

        // Add context type
        sb.Append(HumanizeContext(context));
        sb.Append(' ');

        // Add energy trend from recent check-ins
        var recentCheckIns = state.RecentCheckIns.OrderByDescending(c => c.Date).Take(3).ToList();
        if (recentCheckIns.Count > 0)
        {
            var avgEnergy = recentCheckIns.Where(c => c.EnergyLevel.HasValue).Average(c => c.EnergyLevel!.Value);
            if (avgEnergy < 2.5)
                sb.Append("low energy fatigue tired ");
            else if (avgEnergy > 3.5)
                sb.Append("high energy motivated productive ");
            else
                sb.Append("moderate energy stable ");
        }

        // Add capacity signals from tasks
        var todayTasks = state.Tasks.Where(t => t.ScheduledDate == state.Today).ToList();
        var totalMinutes = todayTasks.Sum(t => t.EstMinutes ?? 0);
        if (totalMinutes > 360)
            sb.Append("overloaded heavy workload capacity ");
        else if (totalMinutes < 60)
            sb.Append("light day available capacity ");

        // Add habit adherence signals
        var lowAdherenceHabits = state.Habits.Where(h => h.Adherence7Day < 0.5m).ToList();
        if (lowAdherenceHabits.Count > 0)
        {
            sb.Append("habit adherence struggle ");
            foreach (var h in lowAdherenceHabits.Take(2))
                sb.Append(h.Title).Append(' ');
        }

        // Add goal momentum signals
        var stalledGoals = state.Goals.Where(g => g.Status == GoalStatus.Active).Take(3);
        foreach (var g in stalledGoals)
        {
            sb.Append(g.Title).Append(' ');
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Builds search query for Strategy stage based on:
    /// - Assessment risks
    /// - Patterns identified
    /// - Momentum state
    /// </summary>
    private static string BuildStrategyQuery(SituationalAssessment assessment, RecommendationContext context)
    {
        var sb = new StringBuilder();

        // Add context type
        sb.Append(HumanizeContext(context));
        sb.Append(' ');

        // Add capacity and momentum state
        sb.Append(assessment.CapacityStatus).Append(' ');
        sb.Append(assessment.OverallMomentum).Append(" momentum ");

        // Add key risks
        foreach (var risk in assessment.KeyRisks.Take(3))
        {
            sb.Append(risk.Area).Append(' ');
            sb.Append(risk.Detail).Append(' ');
        }

        // Add identified patterns
        foreach (var pattern in assessment.Patterns.Take(3))
        {
            sb.Append(pattern).Append(' ');
        }

        // Add goal bottlenecks
        foreach (var goal in assessment.GoalProgressSummary.Take(2))
        {
            if (!string.IsNullOrEmpty(goal.Bottleneck))
            {
                sb.Append(goal.GoalTitle).Append(' ');
                sb.Append(goal.Bottleneck).Append(' ');
            }
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Builds search query for Generation stage based on:
    /// - Domain-specific interventions
    /// - Target entities
    /// </summary>
    private static string BuildGenerationQuery(
        string domain,
        SituationalAssessment assessment,
        IReadOnlyList<InterventionPlanItem> interventions)
    {
        var sb = new StringBuilder();

        // Add domain context
        sb.Append(domain.ToLowerInvariant()).Append(" recommendation ");

        // Add intervention reasoning
        foreach (var intervention in interventions.Take(3))
        {
            sb.Append(intervention.Area).Append(' ');
            sb.Append(intervention.Reasoning).Append(' ');
        }

        // Add momentum context
        sb.Append(assessment.OverallMomentum).Append(" momentum ");

        // Add relevant risks for this domain
        var domainLower = domain.ToLowerInvariant();
        var relevantRisks = assessment.KeyRisks
            .Where(r => r.Area.Contains(domainLower, StringComparison.OrdinalIgnoreCase))
            .Take(2);

        foreach (var risk in relevantRisks)
        {
            sb.Append(risk.Detail).Append(' ');
        }

        return sb.ToString().Trim();
    }

    private static string HumanizeContext(RecommendationContext context) => context switch
    {
        RecommendationContext.MorningCheckIn => "morning check-in planning today",
        RecommendationContext.EveningCheckIn => "evening reflection review",
        RecommendationContext.WeeklyReview => "weekly review trends patterns",
        RecommendationContext.DriftAlert => "drift deviation off-track",
        RecommendationContext.Midday => "midday adjustment",
        RecommendationContext.Onboarding => "onboarding setup new user",
        RecommendationContext.ProactiveCheck => "proactive assessment improvement",
        _ => "general assessment"
    };

    // ─────────────────────────────────────────────────────────────────
    // Core Retrieval Logic
    // ─────────────────────────────────────────────────────────────────

    private async Task<RagContext?> RetrieveContextAsync(
        string userId,
        string queryText,
        RagContextStage stage,
        RagStageOptions stageOptions,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            // Apply timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromMilliseconds(_options.Value.TimeoutMs));

            // Get or generate embedding (with caching)
            var queryVector = await GetOrGenerateEmbeddingAsync(queryText, cts.Token);

            // Search vector store
            var searchResults = await _vectorStore.SearchAsync(
                userId,
                queryVector,
                topK: stageOptions.TopK,
                entityTypes: stageOptions.EntityTypes,
                ct: cts.Token);

            // Filter by similarity threshold and convert to RAG items
            var threshold = _options.Value.SimilarityThreshold;
            var maxTextLength = _options.Value.MaxEmbeddingTextLength;

            var items = searchResults
                .Where(r => r.Score >= threshold)
                .Select(r => new RagContextItem(
                    EntityType: r.EntityType,
                    EntityId: r.EntityId,
                    Title: r.Title,
                    Status: r.Status,
                    EmbeddingText: TruncateText(r.EmbeddingText, maxTextLength),
                    SimilarityScore: r.Score))
                .ToList();

            sw.Stop();

            _logger.LogDebug(
                "RAG retrieval for {Stage}: {Count} items in {ElapsedMs}ms (query: {QueryLength} chars)",
                stage,
                items.Count,
                sw.ElapsedMilliseconds,
                queryText.Length);

            return new RagContext(
                Stage: stage,
                Items: items,
                QueryText: queryText,
                RetrievalLatency: sw.Elapsed);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Original cancellation - rethrow
            throw;
        }
        catch (OperationCanceledException)
        {
            // Timeout - log and continue without RAG
            sw.Stop();
            _logger.LogWarning(
                "RAG retrieval timed out for {Stage} after {ElapsedMs}ms, continuing without context",
                stage,
                sw.ElapsedMilliseconds);
            return null;
        }
        catch (Exception ex)
        {
            // Other errors - log and continue without RAG
            sw.Stop();
            _logger.LogWarning(
                ex,
                "RAG retrieval failed for {Stage} after {ElapsedMs}ms, continuing without context",
                stage,
                sw.ElapsedMilliseconds);
            return null;
        }
    }

    private async Task<float[]> GetOrGenerateEmbeddingAsync(string text, CancellationToken ct)
    {
        if (_embeddingCache.TryGetValue(text, out var cached))
        {
            _logger.LogDebug("Using cached embedding for query");
            return cached;
        }

        var embedding = await _embeddingService.GenerateEmbeddingAsync(text, ct);
        _embeddingCache[text] = embedding;
        return embedding;
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        // Truncate at word boundary if possible
        var truncated = text[..maxLength];
        var lastSpace = truncated.LastIndexOf(' ');

        if (lastSpace > maxLength * 0.7)
            return truncated[..lastSpace] + "...";

        return truncated + "...";
    }
}

using Mastery.Application.Common.Models;
using Mastery.Domain.Diagnostics.Snapshots;
using Mastery.Domain.Entities.Recommendation;
using Mastery.Domain.Entities.Signal;

namespace Mastery.Application.Common.Interfaces;

/// <summary>
/// Orchestrates the full tiered assessment pipeline: Tier 0 → Tier 1 → Tier 2.
/// Decides at each tier whether to escalate to the next tier or stop.
/// </summary>
public interface ITieredAssessmentEngine
{
    /// <summary>
    /// Runs the full tiered assessment pipeline for a batch of signals.
    /// </summary>
    /// <param name="state">The user's current state snapshot.</param>
    /// <param name="signals">The signals being processed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The assessment outcome with recommendations.</returns>
    Task<TieredAssessmentOutcome> AssessAsync(
        UserStateSnapshot state,
        IReadOnlyList<SignalEntry> signals,
        CancellationToken ct = default);
}

/// <summary>
/// The outcome of a tiered assessment, containing all tier results and final recommendations.
/// </summary>
public sealed record TieredAssessmentOutcome(
    string UserId,
    IReadOnlyList<SignalEntry> ProcessedSignals,
    RuleEvaluationResult Tier0Result,
    QuickAssessmentResult? Tier1Result,
    bool Tier2Executed,
    IReadOnlyList<Recommendation> GeneratedRecommendations,
    TieredAssessmentStatistics Statistics,
    DateTime StartedAt,
    DateTime CompletedAt,
    PolicyEnforcementResult PolicyEnforcementResult,
    IReadOnlyList<AgentRun> AgentRuns)
{
    /// <summary>
    /// The final tier that was used for recommendation generation.
    /// </summary>
    public Domain.Enums.AssessmentTier FinalTier => Tier2Executed
        ? Domain.Enums.AssessmentTier.Tier2_FullPipeline
        : Tier1Result != null
            ? Domain.Enums.AssessmentTier.Tier1_QuickAssessment
            : Domain.Enums.AssessmentTier.Tier0_Deterministic;

    /// <summary>
    /// Total processing duration.
    /// </summary>
    public TimeSpan Duration => CompletedAt - StartedAt;
}

/// <summary>
/// Statistics about the tiered assessment execution.
/// </summary>
public sealed record TieredAssessmentStatistics(
    int Tier0RulesEvaluated,
    int Tier0RulesTriggered,
    int Tier0DirectRecommendations,
    decimal? Tier1CombinedScore,
    int Tier1RelevantContextItems,
    int Tier2LlmCallsMade,
    long DurationMs,
    int PolicyRejectionsCount,
    int PolicyViolationsCount);

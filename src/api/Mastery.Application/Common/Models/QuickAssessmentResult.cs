using Mastery.Domain.Enums;

namespace Mastery.Application.Common.Models;

/// <summary>
/// Result of Tier 1 quick assessment, determining whether to escalate to Tier 2.
/// </summary>
public sealed record QuickAssessmentResult(
    string UserId,
    decimal RelevanceScore,
    decimal DeltaScore,
    decimal UrgencyScore,
    decimal CombinedScore,
    bool ShouldEscalateToTier2,
    string? EscalationReason,
    IReadOnlyList<RelevantContextItem> RelevantContext,
    StateDeltaSummary DeltaSummary,
    IReadOnlyList<SignalSummaryItem> SignalSummary,
    DateTime AssessedAt)
{
    /// <summary>
    /// The threshold above which we escalate to Tier 2 (full LLM pipeline).
    /// </summary>
    public const decimal EscalationThreshold = 0.5m;

    /// <summary>
    /// Weight for relevance score in combined calculation.
    /// </summary>
    public const decimal RelevanceWeight = 0.3m;

    /// <summary>
    /// Weight for delta score in combined calculation.
    /// </summary>
    public const decimal DeltaWeight = 0.4m;

    /// <summary>
    /// Weight for urgency score in combined calculation.
    /// </summary>
    public const decimal UrgencyWeight = 0.3m;

    /// <summary>
    /// Calculates the combined score from component scores.
    /// </summary>
    public static decimal CalculateCombinedScore(decimal relevance, decimal delta, decimal urgency) =>
        (relevance * RelevanceWeight) + (delta * DeltaWeight) + (urgency * UrgencyWeight);
}

/// <summary>
/// A context item retrieved via vector search that's relevant to the current signals.
/// </summary>
public sealed record RelevantContextItem(
    string EntityType,
    Guid EntityId,
    string Title,
    string? Status,
    double SimilarityScore,
    string? RelevanceReason);

/// <summary>
/// Summary of state changes since last assessment.
/// </summary>
public sealed record StateDeltaSummary(
    int NewEntitiesCount,
    int ModifiedEntitiesCount,
    int CompletedItemsCount,
    int MissedItemsCount,
    int NewSignalsCount,
    decimal OverallDeltaScore,
    IReadOnlyDictionary<string, int> ChangesByEntityType,
    DateTime? LastAssessmentTime);

/// <summary>
/// Summary of a signal for assessment context.
/// </summary>
public sealed record SignalSummaryItem(
    long SignalId,
    string EventType,
    SignalPriority Priority,
    ProcessingWindowType WindowType,
    string? TargetEntityType,
    Guid? TargetEntityId,
    DateTime CreatedAt);

using Mastery.Domain.Common;
using Mastery.Domain.Enums;

namespace Mastery.Domain.Entities.Signal;

/// <summary>
/// Tracks each signal processing cycle for auditability.
/// Records which signals were processed, the assessment tier used, and outcomes.
/// </summary>
public sealed class SignalProcessingHistory : BaseEntity, IAggregateRoot
{
    /// <summary>
    /// The user ID this processing cycle was for.
    /// </summary>
    public string UserId { get; private set; } = null!;

    /// <summary>
    /// The type of processing window that triggered this cycle.
    /// </summary>
    public ProcessingWindowType WindowType { get; private set; }

    /// <summary>
    /// When this processing cycle started.
    /// </summary>
    public DateTime StartedAt { get; private set; }

    /// <summary>
    /// When this processing cycle completed.
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Number of signals received for processing in this cycle.
    /// </summary>
    public int SignalsReceived { get; private set; }

    /// <summary>
    /// Number of signals successfully processed.
    /// </summary>
    public int SignalsProcessed { get; private set; }

    /// <summary>
    /// Number of signals that were skipped (no action needed).
    /// </summary>
    public int SignalsSkipped { get; private set; }

    /// <summary>
    /// JSON array of signal IDs that were processed in this cycle.
    /// </summary>
    public string? SignalIdsJson { get; private set; }

    /// <summary>
    /// The assessment tier that was ultimately used.
    /// </summary>
    public AssessmentTier FinalTier { get; private set; }

    /// <summary>
    /// Number of Tier 0 deterministic rules that were triggered.
    /// </summary>
    public int Tier0RulesTriggered { get; private set; }

    /// <summary>
    /// Combined score from Tier 1 quick assessment (if executed).
    /// </summary>
    public decimal? Tier1CombinedScore { get; private set; }

    /// <summary>
    /// Whether the full Tier 2 LLM pipeline was executed.
    /// </summary>
    public bool Tier2Executed { get; private set; }

    /// <summary>
    /// Number of recommendations generated in this cycle.
    /// </summary>
    public int RecommendationsGenerated { get; private set; }

    /// <summary>
    /// JSON array of recommendation IDs generated in this cycle.
    /// </summary>
    public string? RecommendationIdsJson { get; private set; }

    /// <summary>
    /// Error message if processing failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// JSON summary of state delta computed during assessment.
    /// </summary>
    public string? StateDeltaSummaryJson { get; private set; }

    /// <summary>
    /// Processing duration in milliseconds.
    /// </summary>
    public long? DurationMs { get; private set; }

    // Private constructor for EF Core
    private SignalProcessingHistory() { }

    /// <summary>
    /// Starts a new processing cycle.
    /// </summary>
    public static SignalProcessingHistory Start(
        string userId,
        ProcessingWindowType windowType,
        DateTime startedAt,
        int signalsReceived,
        IEnumerable<long>? signalIds = null)
    {
        return new SignalProcessingHistory
        {
            UserId = userId,
            WindowType = windowType,
            StartedAt = startedAt,
            SignalsReceived = signalsReceived,
            SignalIdsJson = signalIds != null
                ? System.Text.Json.JsonSerializer.Serialize(signalIds)
                : null,
            FinalTier = AssessmentTier.Skipped
        };
    }

    /// <summary>
    /// Records Tier 0 rule evaluation results.
    /// </summary>
    public void RecordTier0Results(int rulesTriggered)
    {
        Tier0RulesTriggered = rulesTriggered;
        if (rulesTriggered > 0)
        {
            FinalTier = AssessmentTier.Tier0_Deterministic;
        }
    }

    /// <summary>
    /// Records Tier 1 quick assessment results.
    /// </summary>
    public void RecordTier1Results(decimal combinedScore, string? stateDeltaSummary = null)
    {
        Tier1CombinedScore = combinedScore;
        StateDeltaSummaryJson = stateDeltaSummary;
        FinalTier = AssessmentTier.Tier1_QuickAssessment;
    }

    /// <summary>
    /// Records that Tier 2 full pipeline was executed.
    /// </summary>
    public void RecordTier2Executed()
    {
        Tier2Executed = true;
        FinalTier = AssessmentTier.Tier2_FullPipeline;
    }

    /// <summary>
    /// Records the outcome of signal processing.
    /// </summary>
    public void RecordOutcome(int processed, int skipped)
    {
        SignalsProcessed = processed;
        SignalsSkipped = skipped;
    }

    /// <summary>
    /// Records recommendations generated in this cycle.
    /// </summary>
    public void RecordRecommendations(int count, IEnumerable<Guid>? recommendationIds = null)
    {
        RecommendationsGenerated = count;
        RecommendationIdsJson = recommendationIds != null
            ? System.Text.Json.JsonSerializer.Serialize(recommendationIds)
            : null;
    }

    /// <summary>
    /// Records an error that occurred during processing.
    /// </summary>
    public void RecordError(string errorMessage)
    {
        ErrorMessage = errorMessage?.Length > 1000
            ? errorMessage[..1000]
            : errorMessage;
    }

    /// <summary>
    /// Completes the processing cycle.
    /// </summary>
    public void Complete(DateTime completedAt)
    {
        CompletedAt = completedAt;
        DurationMs = (long)(completedAt - StartedAt).TotalMilliseconds;
    }
}

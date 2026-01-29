namespace Mastery.Domain.Enums;

/// <summary>
/// The assessment tier used to process a signal.
/// </summary>
public enum AssessmentTier
{
    /// <summary>
    /// Signal was processed using deterministic rules only (no LLM).
    /// </summary>
    Tier0_Deterministic,

    /// <summary>
    /// Signal was processed using quick vector assessment (lightweight LLM or embeddings).
    /// </summary>
    Tier1_QuickAssessment,

    /// <summary>
    /// Signal was processed using the full 3-stage LLM pipeline.
    /// </summary>
    Tier2_FullPipeline,

    /// <summary>
    /// Signal was skipped (no processing needed).
    /// </summary>
    Skipped,
}

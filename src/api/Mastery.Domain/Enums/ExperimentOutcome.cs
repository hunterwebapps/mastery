namespace Mastery.Domain.Enums;

/// <summary>
/// Classifies the outcome of a completed experiment.
/// </summary>
public enum ExperimentOutcome
{
    /// <summary>
    /// The experiment showed a clear positive effect.
    /// </summary>
    Positive,

    /// <summary>
    /// The experiment showed no meaningful effect.
    /// </summary>
    Neutral,

    /// <summary>
    /// The experiment showed a negative effect.
    /// </summary>
    Negative,

    /// <summary>
    /// Results are inconclusive (e.g., not enough data or too many confounds).
    /// </summary>
    Inconclusive
}

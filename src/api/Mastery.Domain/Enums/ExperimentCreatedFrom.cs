namespace Mastery.Domain.Enums;

/// <summary>
/// Indicates how the experiment was originated.
/// </summary>
public enum ExperimentCreatedFrom
{
    /// <summary>
    /// User created the experiment manually.
    /// </summary>
    Manual,

    /// <summary>
    /// System recommended the experiment during a weekly review.
    /// </summary>
    WeeklyReview,

    /// <summary>
    /// System recommended the experiment from diagnostic analysis.
    /// </summary>
    Diagnostic,

    /// <summary>
    /// Coaching engine suggested the experiment.
    /// </summary>
    Coaching
}

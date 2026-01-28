namespace Mastery.Domain.Enums;

/// <summary>
/// Categorizes what area of personal development the experiment targets.
/// Combines life-area categories with system-specific diagnostic categories.
/// </summary>
public enum ExperimentCategory
{
    // --- Life-area categories ---

    /// <summary>
    /// Experiment related to habit formation, scaling, or modification.
    /// </summary>
    Habit,

    /// <summary>
    /// Experiment related to routine or schedule changes.
    /// </summary>
    Routine,

    /// <summary>
    /// Experiment related to environment or context changes.
    /// </summary>
    Environment,

    /// <summary>
    /// Experiment related to mindset or cognitive strategies.
    /// </summary>
    Mindset,

    /// <summary>
    /// Experiment related to productivity or workflow.
    /// </summary>
    Productivity,

    /// <summary>
    /// Experiment related to health or energy management.
    /// </summary>
    Health,

    /// <summary>
    /// Experiment related to social or relational strategies.
    /// </summary>
    Social,

    // --- System-diagnostic categories ---

    /// <summary>
    /// Testing whether plans are realistic and achievable.
    /// Surfaced by the diagnostic engine when overcommitment is detected.
    /// </summary>
    PlanRealism,

    /// <summary>
    /// Reducing barriers and friction that prevent action.
    /// Surfaced when friction events are frequent.
    /// </summary>
    FrictionReduction,

    /// <summary>
    /// Improving consistency of daily check-ins.
    /// Surfaced when check-in completion rate drops.
    /// </summary>
    CheckInConsistency,

    /// <summary>
    /// Improving follow-through on the daily #1 priority.
    /// Surfaced when top-1 completion rate is low.
    /// </summary>
    Top1FollowThrough,

    /// <summary>
    /// Experiment that doesn't fit other categories.
    /// </summary>
    Other
}

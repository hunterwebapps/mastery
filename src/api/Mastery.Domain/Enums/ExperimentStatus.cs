namespace Mastery.Domain.Enums;

/// <summary>
/// Represents the lifecycle status of an experiment.
/// </summary>
public enum ExperimentStatus
{
    /// <summary>
    /// Experiment is being designed but not yet running.
    /// </summary>
    Draft,

    /// <summary>
    /// Experiment is actively running.
    /// </summary>
    Active,

    /// <summary>
    /// Experiment is temporarily paused.
    /// </summary>
    Paused,

    /// <summary>
    /// Experiment has been completed and results are available.
    /// </summary>
    Completed,

    /// <summary>
    /// Experiment was abandoned before completion.
    /// </summary>
    Abandoned,

    /// <summary>
    /// Experiment is archived and hidden from default views.
    /// </summary>
    Archived
}

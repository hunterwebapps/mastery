namespace Mastery.Domain.Enums;

/// <summary>
/// Represents the type of evaluation window for metric targets.
/// </summary>
public enum WindowType
{
    /// <summary>
    /// Evaluated daily (midnight to midnight in user's timezone).
    /// </summary>
    Daily,

    /// <summary>
    /// Evaluated weekly (Monday to Sunday by default).
    /// </summary>
    Weekly,

    /// <summary>
    /// Evaluated monthly (1st to last day of month).
    /// </summary>
    Monthly,

    /// <summary>
    /// Rolling window of N days from today.
    /// </summary>
    Rolling
}

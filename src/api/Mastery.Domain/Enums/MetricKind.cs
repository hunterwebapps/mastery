namespace Mastery.Domain.Enums;

/// <summary>
/// Represents the role a metric plays in a goal's scoreboard.
/// </summary>
public enum MetricKind
{
    /// <summary>
    /// Outcome metric - what you're trying to achieve (1 per goal recommended).
    /// </summary>
    Lag,

    /// <summary>
    /// Leading indicator - predictive behaviors that drive the outcome (2 per goal recommended).
    /// </summary>
    Lead,

    /// <summary>
    /// Guardrail metric - what not to sacrifice while pursuing the goal (1 per goal recommended).
    /// </summary>
    Constraint
}

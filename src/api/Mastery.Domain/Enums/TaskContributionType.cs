namespace Mastery.Domain.Enums;

/// <summary>
/// Defines how a task completion contributes to a metric observation.
/// Mirrors HabitContributionType for consistency.
/// </summary>
public enum TaskContributionType
{
    /// <summary>
    /// Completion adds 1 to the metric.
    /// Example: "Sales calls made" counter.
    /// </summary>
    BooleanAs1,

    /// <summary>
    /// Completion adds a fixed configured value.
    /// Example: "Write 500 words" adds 500 to WordsWritten metric.
    /// </summary>
    FixedValue,

    /// <summary>
    /// Uses the ActualMinutes from completion as the metric value.
    /// Example: "Deep work block" writes to DeepWorkMinutes metric.
    /// </summary>
    UseActualMinutes,

    /// <summary>
    /// User enters the value at completion time.
    /// Example: Task prompts for actual count/amount.
    /// </summary>
    UseEnteredValue
}

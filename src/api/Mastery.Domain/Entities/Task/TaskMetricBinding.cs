using Mastery.Domain.Common;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;

namespace Mastery.Domain.Entities.Task;

/// <summary>
/// Represents the binding between a task and a metric.
/// When the task is completed, this binding determines how to record the metric observation.
/// </summary>
public sealed class TaskMetricBinding : AuditableEntity
{
    /// <summary>
    /// The task this binding belongs to.
    /// </summary>
    public Guid TaskId { get; private set; }

    /// <summary>
    /// The metric definition this task contributes to.
    /// </summary>
    public Guid MetricDefinitionId { get; private set; }

    /// <summary>
    /// How the task completion contributes to the metric.
    /// </summary>
    public TaskContributionType ContributionType { get; private set; }

    /// <summary>
    /// The fixed value to use when ContributionType is FixedValue.
    /// </summary>
    public decimal? FixedValue { get; private set; }

    /// <summary>
    /// Optional notes about this binding.
    /// </summary>
    public string? Notes { get; private set; }

    private TaskMetricBinding() { } // EF Core

    public static TaskMetricBinding Create(
        Guid taskId,
        Guid metricDefinitionId,
        TaskContributionType contributionType,
        decimal? fixedValue = null,
        string? notes = null)
    {
        if (taskId == Guid.Empty)
            throw new DomainException("TaskId cannot be empty.");

        if (metricDefinitionId == Guid.Empty)
            throw new DomainException("MetricDefinitionId cannot be empty.");

        if (contributionType == TaskContributionType.FixedValue && !fixedValue.HasValue)
            throw new DomainException("FixedValue is required when contribution type is FixedValue.");

        if (contributionType != TaskContributionType.FixedValue && fixedValue.HasValue)
            throw new DomainException("FixedValue should only be set when contribution type is FixedValue.");

        return new TaskMetricBinding
        {
            TaskId = taskId,
            MetricDefinitionId = metricDefinitionId,
            ContributionType = contributionType,
            FixedValue = fixedValue,
            Notes = notes
        };
    }

    public void Update(
        TaskContributionType contributionType,
        decimal? fixedValue = null,
        string? notes = null)
    {
        if (contributionType == TaskContributionType.FixedValue && !fixedValue.HasValue)
            throw new DomainException("FixedValue is required when contribution type is FixedValue.");

        if (contributionType != TaskContributionType.FixedValue && fixedValue.HasValue)
            throw new DomainException("FixedValue should only be set when contribution type is FixedValue.");

        ContributionType = contributionType;
        FixedValue = fixedValue;
        Notes = notes;
    }

    /// <summary>
    /// Calculates the metric value based on completion and contribution type.
    /// </summary>
    public decimal GetContributionValue(int? actualMinutes = null, decimal? enteredValue = null)
    {
        return ContributionType switch
        {
            TaskContributionType.BooleanAs1 => 1m,
            TaskContributionType.FixedValue => FixedValue ?? 1m,
            TaskContributionType.UseActualMinutes => actualMinutes
                ?? throw new DomainException("Actual minutes is required for UseActualMinutes contribution type."),
            TaskContributionType.UseEnteredValue => enteredValue
                ?? throw new DomainException("Entered value is required for UseEnteredValue contribution type."),
            _ => throw new DomainException($"Unknown contribution type: {ContributionType}")
        };
    }
}

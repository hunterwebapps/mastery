using Mastery.Domain.Common;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;

namespace Mastery.Domain.Entities.Habit;

/// <summary>
/// Represents the binding between a habit and a metric.
/// When the habit is completed, this binding determines how to record the metric observation.
/// </summary>
public sealed class HabitMetricBinding : AuditableEntity
{
    /// <summary>
    /// The habit this binding belongs to.
    /// </summary>
    public Guid HabitId { get; private set; }

    /// <summary>
    /// The metric definition this habit contributes to.
    /// </summary>
    public Guid MetricDefinitionId { get; private set; }

    /// <summary>
    /// How the habit completion contributes to the metric.
    /// </summary>
    public HabitContributionType ContributionType { get; private set; }

    /// <summary>
    /// The fixed value to use when ContributionType is FixedValue.
    /// </summary>
    public decimal? FixedValue { get; private set; }

    /// <summary>
    /// Optional notes about this binding.
    /// </summary>
    public string? Notes { get; private set; }

    private HabitMetricBinding() { } // EF Core

    public static HabitMetricBinding Create(
        Guid habitId,
        Guid metricDefinitionId,
        HabitContributionType contributionType,
        decimal? fixedValue = null,
        string? notes = null)
    {
        if (habitId == Guid.Empty)
            throw new DomainException("HabitId cannot be empty.");

        if (metricDefinitionId == Guid.Empty)
            throw new DomainException("MetricDefinitionId cannot be empty.");

        if (contributionType == HabitContributionType.FixedValue && !fixedValue.HasValue)
            throw new DomainException("FixedValue is required when contribution type is FixedValue.");

        if (contributionType != HabitContributionType.FixedValue && fixedValue.HasValue)
            throw new DomainException("FixedValue should only be set when contribution type is FixedValue.");

        return new HabitMetricBinding
        {
            HabitId = habitId,
            MetricDefinitionId = metricDefinitionId,
            ContributionType = contributionType,
            FixedValue = fixedValue,
            Notes = notes
        };
    }

    public void Update(
        HabitContributionType contributionType,
        decimal? fixedValue = null,
        string? notes = null)
    {
        if (contributionType == HabitContributionType.FixedValue && !fixedValue.HasValue)
            throw new DomainException("FixedValue is required when contribution type is FixedValue.");

        if (contributionType != HabitContributionType.FixedValue && fixedValue.HasValue)
            throw new DomainException("FixedValue should only be set when contribution type is FixedValue.");

        ContributionType = contributionType;
        FixedValue = fixedValue;
        Notes = notes;
    }

    /// <summary>
    /// Calculates the metric value based on completion and contribution type.
    /// </summary>
    public decimal GetContributionValue(decimal? enteredValue = null)
    {
        return ContributionType switch
        {
            HabitContributionType.BooleanAs1 => 1m,
            HabitContributionType.FixedValue => FixedValue ?? 1m,
            HabitContributionType.UseEnteredValue => enteredValue
                ?? throw new DomainException("Entered value is required for UseEnteredValue contribution type."),
            _ => throw new DomainException($"Unknown contribution type: {ContributionType}")
        };
    }
}

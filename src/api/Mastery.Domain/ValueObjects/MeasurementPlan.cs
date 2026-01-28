using System.Text.Json.Serialization;
using Mastery.Domain.Common;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;

namespace Mastery.Domain.ValueObjects;

/// <summary>
/// Represents the measurement configuration for an experiment, defining what to measure and how.
/// </summary>
public sealed class MeasurementPlan : ValueObject
{
    private const int MinWindowDays = 1;
    private const int MaxWindowDays = 90;
    private const decimal MinCompliance = 0m;
    private const decimal MaxCompliance = 1m;
    private const decimal DefaultComplianceThreshold = 0.7m;
    private const int DefaultWindowDays = 7;

    /// <summary>
    /// The primary metric to track for this experiment.
    /// </summary>
    public Guid PrimaryMetricDefinitionId { get; }

    /// <summary>
    /// How the primary metric should be aggregated over the evaluation window.
    /// </summary>
    public MetricAggregation PrimaryAggregation { get; }

    /// <summary>
    /// Number of days before the experiment starts to establish a baseline.
    /// </summary>
    public int BaselineWindowDays { get; }

    /// <summary>
    /// Number of days the experiment runs to collect data.
    /// </summary>
    public int RunWindowDays { get; }

    /// <summary>
    /// Additional metrics to monitor for unintended side effects.
    /// </summary>
    public List<Guid> GuardrailMetricDefinitionIds { get; }

    /// <summary>
    /// Minimum fraction of days with data required for the experiment to be considered valid (0.0 to 1.0).
    /// </summary>
    public decimal MinComplianceThreshold { get; }

    // Required for EF Core and JSON deserialization
    private MeasurementPlan()
    {
        PrimaryMetricDefinitionId = Guid.Empty;
        PrimaryAggregation = MetricAggregation.Average;
        BaselineWindowDays = DefaultWindowDays;
        RunWindowDays = DefaultWindowDays;
        GuardrailMetricDefinitionIds = [];
        MinComplianceThreshold = DefaultComplianceThreshold;
    }

    [JsonConstructor]
    public MeasurementPlan(
        Guid primaryMetricDefinitionId,
        MetricAggregation primaryAggregation,
        int baselineWindowDays,
        int runWindowDays,
        List<Guid> guardrailMetricDefinitionIds,
        decimal minComplianceThreshold)
    {
        PrimaryMetricDefinitionId = primaryMetricDefinitionId;
        PrimaryAggregation = primaryAggregation;
        BaselineWindowDays = baselineWindowDays;
        RunWindowDays = runWindowDays;
        GuardrailMetricDefinitionIds = guardrailMetricDefinitionIds ?? [];
        MinComplianceThreshold = minComplianceThreshold;
    }

    /// <summary>
    /// Creates a new MeasurementPlan with validation.
    /// </summary>
    public static MeasurementPlan Create(
        Guid primaryMetricDefinitionId,
        MetricAggregation primaryAggregation,
        int baselineWindowDays = DefaultWindowDays,
        int runWindowDays = DefaultWindowDays,
        List<Guid>? guardrailMetricDefinitionIds = null,
        decimal minComplianceThreshold = DefaultComplianceThreshold)
    {
        if (primaryMetricDefinitionId == Guid.Empty)
            throw new DomainException("Primary metric definition ID is required.");

        if (!Enum.IsDefined(primaryAggregation))
            throw new DomainException($"Invalid metric aggregation: {primaryAggregation}.");

        if (baselineWindowDays < MinWindowDays || baselineWindowDays > MaxWindowDays)
            throw new DomainException($"Baseline window days must be between {MinWindowDays} and {MaxWindowDays}.");

        if (runWindowDays < MinWindowDays || runWindowDays > MaxWindowDays)
            throw new DomainException($"Run window days must be between {MinWindowDays} and {MaxWindowDays}.");

        if (minComplianceThreshold < MinCompliance || minComplianceThreshold > MaxCompliance)
            throw new DomainException($"Minimum compliance threshold must be between {MinCompliance} and {MaxCompliance}.");

        return new MeasurementPlan(
            primaryMetricDefinitionId,
            primaryAggregation,
            baselineWindowDays,
            runWindowDays,
            guardrailMetricDefinitionIds ?? [],
            minComplianceThreshold);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return PrimaryMetricDefinitionId;
        yield return PrimaryAggregation;
        yield return BaselineWindowDays;
        yield return RunWindowDays;
        yield return string.Join(",", GuardrailMetricDefinitionIds.OrderBy(id => id));
        yield return MinComplianceThreshold;
    }

    public override string ToString() =>
        $"Measure {PrimaryAggregation} over {RunWindowDays}d (baseline: {BaselineWindowDays}d, compliance: {MinComplianceThreshold:P0})";
}

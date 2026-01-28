using Mastery.Domain.Common;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;

namespace Mastery.Domain.Entities.Experiment;

/// <summary>
/// Represents the measured result of a completed experiment.
/// Captures baseline vs. run values, delta calculations, and outcome classification.
/// </summary>
public sealed class ExperimentResult : BaseEntity
{
    /// <summary>
    /// The experiment this result belongs to.
    /// </summary>
    public Guid ExperimentId { get; private set; }

    /// <summary>
    /// The metric value before the experiment started.
    /// </summary>
    public decimal? BaselineValue { get; private set; }

    /// <summary>
    /// The metric value during/after the experiment run.
    /// </summary>
    public decimal? RunValue { get; private set; }

    /// <summary>
    /// Absolute change (RunValue - BaselineValue).
    /// </summary>
    public decimal? Delta { get; private set; }

    /// <summary>
    /// Percentage change relative to baseline.
    /// </summary>
    public decimal? DeltaPercent { get; private set; }

    /// <summary>
    /// Classification of the experiment outcome.
    /// </summary>
    public ExperimentOutcome OutcomeClassification { get; private set; }

    /// <summary>
    /// How well the user adhered to the experiment protocol (0.0 to 1.0).
    /// </summary>
    public decimal? ComplianceRate { get; private set; }

    /// <summary>
    /// Human-readable or AI-generated summary of the results.
    /// </summary>
    public string? NarrativeSummary { get; private set; }

    /// <summary>
    /// When this result was computed.
    /// </summary>
    public DateTime ComputedAt { get; private set; }

    private ExperimentResult() { } // EF Core

    public static ExperimentResult Create(
        Guid experimentId,
        ExperimentOutcome outcomeClassification,
        decimal? baselineValue = null,
        decimal? runValue = null,
        decimal? delta = null,
        decimal? deltaPercent = null,
        decimal? complianceRate = null,
        string? narrativeSummary = null)
    {
        if (experimentId == Guid.Empty)
            throw new DomainException("ExperimentId cannot be empty.");

        if (complianceRate.HasValue && (complianceRate.Value < 0 || complianceRate.Value > 1))
            throw new DomainException("Compliance rate must be between 0 and 1.");

        if (narrativeSummary != null && narrativeSummary.Length > 4000)
            throw new DomainException("Narrative summary cannot exceed 4000 characters.");

        return new ExperimentResult
        {
            ExperimentId = experimentId,
            BaselineValue = baselineValue,
            RunValue = runValue,
            Delta = delta,
            DeltaPercent = deltaPercent,
            OutcomeClassification = outcomeClassification,
            ComplianceRate = complianceRate,
            NarrativeSummary = narrativeSummary,
            ComputedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Recomputes the result with updated values.
    /// </summary>
    public void Recompute(
        ExperimentOutcome outcomeClassification,
        decimal? baselineValue = null,
        decimal? runValue = null,
        decimal? delta = null,
        decimal? deltaPercent = null,
        decimal? complianceRate = null,
        string? narrativeSummary = null)
    {
        if (complianceRate.HasValue && (complianceRate.Value < 0 || complianceRate.Value > 1))
            throw new DomainException("Compliance rate must be between 0 and 1.");

        if (narrativeSummary != null && narrativeSummary.Length > 4000)
            throw new DomainException("Narrative summary cannot exceed 4000 characters.");

        BaselineValue = baselineValue;
        RunValue = runValue;
        Delta = delta;
        DeltaPercent = deltaPercent;
        OutcomeClassification = outcomeClassification;
        ComplianceRate = complianceRate;
        NarrativeSummary = narrativeSummary;
        ComputedAt = DateTime.UtcNow;
    }
}

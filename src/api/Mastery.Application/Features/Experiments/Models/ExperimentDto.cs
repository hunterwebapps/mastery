namespace Mastery.Application.Features.Experiments.Models;

/// <summary>
/// Full experiment DTO with all details including hypothesis, measurement plan, notes, and result.
/// </summary>
public sealed record ExperimentDto
{
    public Guid Id { get; init; }
    public required string UserId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string Category { get; init; }
    public required string Status { get; init; }
    public required string CreatedFrom { get; init; }
    public required HypothesisDto Hypothesis { get; init; }
    public required MeasurementPlanDto MeasurementPlan { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDatePlanned { get; init; }
    public DateOnly? EndDateActual { get; init; }
    public List<Guid> LinkedGoalIds { get; init; } = [];
    public List<ExperimentNoteDto> Notes { get; init; } = [];
    public ExperimentResultDto? Result { get; init; }
    public int? DaysRemaining { get; init; }
    public int? DaysElapsed { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
}

/// <summary>
/// Experiment summary for list views - lightweight without nested details.
/// </summary>
public sealed record ExperimentSummaryDto
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Category { get; init; }
    public required string Status { get; init; }
    public required string CreatedFrom { get; init; }
    public required string HypothesisSummary { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDatePlanned { get; init; }
    public int? DaysRemaining { get; init; }
    public int? DaysElapsed { get; init; }
    public string? OutcomeClassification { get; init; }
    public int NoteCount { get; init; }
    public bool HasResult { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Hypothesis describing the expected change and outcome.
/// </summary>
public sealed record HypothesisDto
{
    public required string Change { get; init; }
    public required string ExpectedOutcome { get; init; }
    public string? Rationale { get; init; }
    public required string Summary { get; init; }
}

/// <summary>
/// Measurement plan defining how the experiment is evaluated.
/// </summary>
public sealed record MeasurementPlanDto
{
    public Guid PrimaryMetricDefinitionId { get; init; }
    public required string PrimaryAggregation { get; init; }
    public int BaselineWindowDays { get; init; }
    public int RunWindowDays { get; init; }
    public List<Guid> GuardrailMetricDefinitionIds { get; init; } = [];
    public decimal MinComplianceThreshold { get; init; }
}

/// <summary>
/// A note attached to an experiment.
/// </summary>
public sealed record ExperimentNoteDto
{
    public Guid Id { get; init; }
    public required string Content { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Computed result of an experiment after conclusion.
/// </summary>
public sealed record ExperimentResultDto
{
    public Guid Id { get; init; }
    public decimal? BaselineValue { get; init; }
    public decimal? RunValue { get; init; }
    public decimal? Delta { get; init; }
    public decimal? DeltaPercent { get; init; }
    public required string OutcomeClassification { get; init; }
    public decimal? ComplianceRate { get; init; }
    public string? NarrativeSummary { get; init; }
    public DateTime ComputedAt { get; init; }
}

namespace Mastery.Api.Contracts.Experiments;

/// <summary>
/// Request to create a new experiment.
/// </summary>
public sealed record CreateExperimentRequest(
    string Title,
    string Category,
    string CreatedFrom,
    CreateHypothesisRequest Hypothesis,
    CreateMeasurementPlanRequest MeasurementPlan,
    string? Description = null,
    List<Guid>? LinkedGoalIds = null,
    DateOnly? StartDate = null,
    DateOnly? EndDatePlanned = null);

/// <summary>
/// Request to create a hypothesis.
/// </summary>
public sealed record CreateHypothesisRequest(
    string Change,
    string ExpectedOutcome,
    string? Rationale = null);

/// <summary>
/// Request to create a measurement plan.
/// </summary>
public sealed record CreateMeasurementPlanRequest(
    Guid PrimaryMetricDefinitionId,
    string PrimaryAggregation,
    int BaselineWindowDays = 7,
    int RunWindowDays = 7,
    List<Guid>? GuardrailMetricDefinitionIds = null,
    decimal MinComplianceThreshold = 0.7m);

/// <summary>
/// Request to update an experiment (draft only).
/// </summary>
public sealed record UpdateExperimentRequest(
    string? Title = null,
    string? Description = null,
    string? Category = null,
    CreateHypothesisRequest? Hypothesis = null,
    CreateMeasurementPlanRequest? MeasurementPlan = null,
    List<Guid>? LinkedGoalIds = null,
    DateOnly? StartDate = null,
    DateOnly? EndDatePlanned = null);

/// <summary>
/// Request to complete an experiment with results.
/// </summary>
public sealed record CompleteExperimentRequest(
    string OutcomeClassification,
    decimal? BaselineValue = null,
    decimal? RunValue = null,
    decimal? ComplianceRate = null,
    string? NarrativeSummary = null);

/// <summary>
/// Request to abandon an experiment.
/// </summary>
public sealed record AbandonExperimentRequest(
    string? Reason = null);

/// <summary>
/// Request to add a note to an experiment.
/// </summary>
public sealed record AddExperimentNoteRequest(
    string Content);

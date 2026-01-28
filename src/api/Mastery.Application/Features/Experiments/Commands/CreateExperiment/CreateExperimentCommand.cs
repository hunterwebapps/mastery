using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Experiments.Commands.CreateExperiment;

/// <summary>
/// Creates a new experiment for the current user.
/// </summary>
public sealed record CreateExperimentCommand(
    string Title,
    string Category,
    string CreatedFrom,
    CreateHypothesisInput Hypothesis,
    CreateMeasurementPlanInput MeasurementPlan,
    string? Description = null,
    List<Guid>? LinkedGoalIds = null,
    DateOnly? StartDate = null,
    DateOnly? EndDatePlanned = null) : ICommand<Guid>;

/// <summary>
/// Input for creating a hypothesis.
/// </summary>
public sealed record CreateHypothesisInput(
    string Change,
    string ExpectedOutcome,
    string? Rationale = null);

/// <summary>
/// Input for creating a measurement plan.
/// </summary>
public sealed record CreateMeasurementPlanInput(
    Guid PrimaryMetricDefinitionId,
    string PrimaryAggregation,
    int BaselineWindowDays = 7,
    int RunWindowDays = 7,
    List<Guid>? GuardrailMetricDefinitionIds = null,
    decimal MinComplianceThreshold = 0.7m);

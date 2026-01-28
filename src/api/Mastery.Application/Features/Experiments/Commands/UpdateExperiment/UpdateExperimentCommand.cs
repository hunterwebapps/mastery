using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Experiments.Commands.CreateExperiment;

namespace Mastery.Application.Features.Experiments.Commands.UpdateExperiment;

/// <summary>
/// Updates an existing experiment. Only allowed for experiments in Draft status.
/// </summary>
public sealed record UpdateExperimentCommand(
    Guid Id,
    string? Title = null,
    string? Description = null,
    string? Category = null,
    CreateHypothesisInput? Hypothesis = null,
    CreateMeasurementPlanInput? MeasurementPlan = null,
    List<Guid>? LinkedGoalIds = null,
    DateOnly? StartDate = null,
    DateOnly? EndDatePlanned = null) : ICommand;

using Mastery.Application.Common.Interfaces;

namespace Mastery.Application.Features.Experiments.Commands.CompleteExperiment;

/// <summary>
/// Completes an experiment with a result.
/// </summary>
public sealed record CompleteExperimentCommand(
    Guid Id,
    string OutcomeClassification,
    decimal? BaselineValue = null,
    decimal? RunValue = null,
    decimal? ComplianceRate = null,
    string? NarrativeSummary = null) : ICommand;

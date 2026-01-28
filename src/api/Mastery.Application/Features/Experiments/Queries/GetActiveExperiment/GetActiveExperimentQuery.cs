using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Experiments.Models;

namespace Mastery.Application.Features.Experiments.Queries.GetActiveExperiment;

/// <summary>
/// Gets the currently active experiment for the current user, or null if none.
/// </summary>
public sealed record GetActiveExperimentQuery() : IQuery<ExperimentDto?>;

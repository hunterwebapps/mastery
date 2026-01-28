using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Experiments.Models;

namespace Mastery.Application.Features.Experiments.Queries.GetExperiments;

/// <summary>
/// Gets all experiments for the current user, optionally filtered by status.
/// </summary>
public sealed record GetExperimentsQuery(string? Status = null) : IQuery<IReadOnlyList<ExperimentSummaryDto>>;

using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Experiments.Models;

namespace Mastery.Application.Features.Experiments.Queries.GetExperimentById;

/// <summary>
/// Gets a single experiment by ID with all details including hypothesis, measurement plan, notes, and result.
/// </summary>
public sealed record GetExperimentByIdQuery(Guid Id) : IQuery<ExperimentDto>;

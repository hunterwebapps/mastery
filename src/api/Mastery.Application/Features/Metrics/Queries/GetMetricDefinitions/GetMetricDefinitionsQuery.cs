using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Metrics.Models;

namespace Mastery.Application.Features.Metrics.Queries.GetMetricDefinitions;

/// <summary>
/// Gets all metric definitions for the current user.
/// </summary>
public sealed record GetMetricDefinitionsQuery(bool IncludeArchived = false) : IQuery<IReadOnlyList<MetricDefinitionDto>>;

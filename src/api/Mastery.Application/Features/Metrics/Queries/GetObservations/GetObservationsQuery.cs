using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Metrics.Models;

namespace Mastery.Application.Features.Metrics.Queries.GetObservations;

/// <summary>
/// Gets observations for a metric within a date range.
/// </summary>
public sealed record GetObservationsQuery(
    Guid MetricDefinitionId,
    DateOnly StartDate,
    DateOnly EndDate) : IQuery<MetricTimeSeriesDto>;

using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Metrics.Models;
using Mastery.Domain.Entities.Metrics;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Metrics.Queries.GetObservations;

public sealed class GetObservationsQueryHandler : IQueryHandler<GetObservationsQuery, MetricTimeSeriesDto>
{
    private readonly IMetricDefinitionRepository _metricDefinitionRepository;
    private readonly IMetricObservationRepository _observationRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetObservationsQueryHandler(
        IMetricDefinitionRepository metricDefinitionRepository,
        IMetricObservationRepository observationRepository,
        ICurrentUserService currentUserService)
    {
        _metricDefinitionRepository = metricDefinitionRepository;
        _observationRepository = observationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<MetricTimeSeriesDto> Handle(GetObservationsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        // Verify metric definition exists and belongs to user
        var metricDefinition = await _metricDefinitionRepository.GetByIdAsync(request.MetricDefinitionId, cancellationToken)
            ?? throw new NotFoundException(nameof(MetricDefinition), request.MetricDefinitionId);

        if (metricDefinition.UserId != userId)
            throw new DomainException("Metric definition does not belong to the current user.");

        var observations = await _observationRepository.GetByMetricAndDateRangeAsync(
            request.MetricDefinitionId,
            userId,
            request.StartDate,
            request.EndDate,
            excludeCorrected: true,
            cancellationToken);

        // Get aggregated value using the metric's default aggregation
        var aggregatedValue = await _observationRepository.GetAggregatedValueAsync(
            request.MetricDefinitionId,
            userId,
            request.StartDate,
            request.EndDate,
            metricDefinition.DefaultAggregation,
            cancellationToken);

        return new MetricTimeSeriesDto
        {
            MetricDefinitionId = request.MetricDefinitionId,
            MetricName = metricDefinition.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DataPoints = observations.Select(MapToDataPoint).ToList(),
            AggregatedValue = aggregatedValue,
            Count = observations.Count
        };
    }

    private static MetricDataPointDto MapToDataPoint(MetricObservation observation)
    {
        return new MetricDataPointDto
        {
            Date = observation.ObservedOn,
            Value = observation.Value,
            Note = observation.Note
        };
    }
}

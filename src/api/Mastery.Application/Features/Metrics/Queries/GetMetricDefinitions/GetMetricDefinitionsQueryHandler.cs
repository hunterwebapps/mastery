using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Metrics.Models;
using Mastery.Domain.Entities.Metrics;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Metrics.Queries.GetMetricDefinitions;

public sealed class GetMetricDefinitionsQueryHandler : IQueryHandler<GetMetricDefinitionsQuery, IReadOnlyList<MetricDefinitionDto>>
{
    private readonly IMetricDefinitionRepository _metricDefinitionRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetMetricDefinitionsQueryHandler(
        IMetricDefinitionRepository metricDefinitionRepository,
        ICurrentUserService currentUserService)
    {
        _metricDefinitionRepository = metricDefinitionRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<MetricDefinitionDto>> Handle(GetMetricDefinitionsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return [];

        var definitions = await _metricDefinitionRepository.GetByUserIdAsync(
            userId,
            request.IncludeArchived,
            cancellationToken);

        return definitions.Select(MapToDto).ToList();
    }

    private static MetricDefinitionDto MapToDto(MetricDefinition definition)
    {
        return new MetricDefinitionDto
        {
            Id = definition.Id,
            UserId = definition.UserId,
            Name = definition.Name,
            Description = definition.Description,
            DataType = definition.DataType.ToString(),
            Unit = definition.Unit != null ? new MetricUnitDto
            {
                Type = definition.Unit.UnitType,
                Label = definition.Unit.DisplayLabel
            } : null,
            Direction = definition.Direction.ToString(),
            DefaultCadence = definition.DefaultCadence.ToString(),
            DefaultAggregation = definition.DefaultAggregation.ToString(),
            IsArchived = definition.IsArchived,
            Tags = definition.Tags.ToList(),
            CreatedAt = definition.CreatedAt,
            ModifiedAt = definition.ModifiedAt
        };
    }
}

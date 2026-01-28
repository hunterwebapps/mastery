using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Metrics;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;
using Mastery.Domain.ValueObjects;

namespace Mastery.Application.Features.Metrics.Commands.CreateMetricDefinition;

public sealed class CreateMetricDefinitionCommandHandler : ICommandHandler<CreateMetricDefinitionCommand, Guid>
{
    private readonly IMetricDefinitionRepository _metricDefinitionRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateMetricDefinitionCommandHandler(
        IMetricDefinitionRepository metricDefinitionRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _metricDefinitionRepository = metricDefinitionRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateMetricDefinitionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        // Check for duplicate name
        if (await _metricDefinitionRepository.ExistsByUserIdAndNameAsync(userId, request.Name, cancellationToken))
            throw new DomainException($"A metric with the name '{request.Name}' already exists.");

        // Parse enums
        if (!Enum.TryParse<MetricDataType>(request.DataType, out var dataType))
            throw new DomainException($"Invalid data type: {request.DataType}");

        if (!Enum.TryParse<MetricDirection>(request.Direction, out var direction))
            throw new DomainException($"Invalid direction: {request.Direction}");

        if (!Enum.TryParse<WindowType>(request.DefaultCadence, out var defaultCadence))
            throw new DomainException($"Invalid cadence: {request.DefaultCadence}");

        if (!Enum.TryParse<MetricAggregation>(request.DefaultAggregation, out var defaultAggregation))
            throw new DomainException($"Invalid aggregation: {request.DefaultAggregation}");

        // Create unit (required, default to None if not provided)
        var unit = request.Unit != null
            ? MetricUnit.Create(request.Unit.Type, request.Unit.Label)
            : MetricUnit.None;

        var metricDefinition = MetricDefinition.Create(
            userId: userId,
            name: request.Name,
            description: request.Description,
            dataType: dataType,
            unit: unit,
            direction: direction,
            defaultCadence: defaultCadence,
            defaultAggregation: defaultAggregation,
            tags: request.Tags);

        await _metricDefinitionRepository.AddAsync(metricDefinition, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return metricDefinition.Id;
    }
}

using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;
using Mastery.Domain.ValueObjects;

namespace Mastery.Application.Features.Metrics.Commands.UpdateMetricDefinition;

public sealed class UpdateMetricDefinitionCommandHandler : ICommandHandler<UpdateMetricDefinitionCommand>
{
    private readonly IMetricDefinitionRepository _metricDefinitionRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMetricDefinitionCommandHandler(
        IMetricDefinitionRepository metricDefinitionRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _metricDefinitionRepository = metricDefinitionRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateMetricDefinitionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var metricDefinition = await _metricDefinitionRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Metrics.MetricDefinition), request.Id);

        if (metricDefinition.UserId != userId)
            throw new DomainException("Metric definition does not belong to the current user.");

        // Check for duplicate name (if name changed)
        if (metricDefinition.Name != request.Name)
        {
            if (await _metricDefinitionRepository.ExistsByUserIdAndNameAsync(userId, request.Name, cancellationToken))
                throw new DomainException($"A metric with the name '{request.Name}' already exists.");
        }

        // Parse enums
        if (!Enum.TryParse<MetricDataType>(request.DataType, out var dataType))
            throw new DomainException($"Invalid data type: {request.DataType}");

        if (!Enum.TryParse<MetricDirection>(request.Direction, out var direction))
            throw new DomainException($"Invalid direction: {request.Direction}");

        if (!Enum.TryParse<WindowType>(request.DefaultCadence, out var defaultCadence))
            throw new DomainException($"Invalid cadence: {request.DefaultCadence}");

        if (!Enum.TryParse<MetricAggregation>(request.DefaultAggregation, out var defaultAggregation))
            throw new DomainException($"Invalid aggregation: {request.DefaultAggregation}");

        // Create unit if provided
        var unit = request.Unit != null
            ? MetricUnit.Create(request.Unit.Type, request.Unit.Label)
            : null;

        metricDefinition.Update(
            name: request.Name,
            description: request.Description,
            unit: unit,
            direction: direction,
            defaultCadence: defaultCadence,
            defaultAggregation: defaultAggregation,
            tags: request.Tags);

        if (request.IsArchived && !metricDefinition.IsArchived)
        {
            metricDefinition.Archive();
        }
        else if (!request.IsArchived && metricDefinition.IsArchived)
        {
            metricDefinition.Unarchive();
        }

        await _metricDefinitionRepository.UpdateAsync(metricDefinition, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

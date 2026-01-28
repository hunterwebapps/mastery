using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Metrics;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Metrics.Commands.RecordObservation;

public sealed class RecordObservationCommandHandler : ICommandHandler<RecordObservationCommand, Guid>
{
    private readonly IMetricDefinitionRepository _metricDefinitionRepository;
    private readonly IMetricObservationRepository _observationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RecordObservationCommandHandler(
        IMetricDefinitionRepository metricDefinitionRepository,
        IMetricObservationRepository observationRepository,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _metricDefinitionRepository = metricDefinitionRepository;
        _observationRepository = observationRepository;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(RecordObservationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        // Verify metric definition exists and belongs to user
        var metricDefinition = await _metricDefinitionRepository.GetByIdAsync(request.MetricDefinitionId, cancellationToken)
            ?? throw new NotFoundException(nameof(MetricDefinition), request.MetricDefinitionId);

        if (metricDefinition.UserId != userId)
            throw new DomainException("Metric definition does not belong to the current user.");

        if (metricDefinition.IsArchived)
            throw new DomainException("Cannot record observations for archived metrics.");

        // Parse source
        if (!Enum.TryParse<MetricSourceType>(request.Source, out var source))
            throw new DomainException($"Invalid source type: {request.Source}");

        var now = _dateTimeProvider.UtcNow;
        var observedOn = request.ObservedOn ?? DateOnly.FromDateTime(now);

        var observation = MetricObservation.Create(
            metricDefinitionId: request.MetricDefinitionId,
            userId: userId,
            observedAt: now,
            observedOn: observedOn,
            value: request.Value,
            source: source,
            correlationId: request.CorrelationId,
            note: request.Note);

        await _observationRepository.AddAsync(observation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return observation.Id;
    }
}

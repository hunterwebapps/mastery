using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Experiment;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;
using Mastery.Domain.ValueObjects;

namespace Mastery.Application.Features.Experiments.Commands.CreateExperiment;

public sealed class CreateExperimentCommandHandler : ICommandHandler<CreateExperimentCommand, Guid>
{
    private readonly IExperimentRepository _experimentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateExperimentCommandHandler(
        IExperimentRepository experimentRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _experimentRepository = experimentRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateExperimentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        // Parse enums
        if (!Enum.TryParse<ExperimentCategory>(request.Category, out var category))
            throw new DomainException($"Invalid experiment category: {request.Category}");

        if (!Enum.TryParse<ExperimentCreatedFrom>(request.CreatedFrom, out var createdFrom))
            throw new DomainException($"Invalid experiment created from: {request.CreatedFrom}");

        if (!Enum.TryParse<MetricAggregation>(request.MeasurementPlan.PrimaryAggregation, out var aggregation))
            throw new DomainException($"Invalid metric aggregation: {request.MeasurementPlan.PrimaryAggregation}");

        // Create value objects
        var hypothesis = Hypothesis.Create(
            change: request.Hypothesis.Change,
            expectedOutcome: request.Hypothesis.ExpectedOutcome,
            rationale: request.Hypothesis.Rationale);

        var measurementPlan = MeasurementPlan.Create(
            primaryMetricDefinitionId: request.MeasurementPlan.PrimaryMetricDefinitionId,
            primaryAggregation: aggregation,
            baselineWindowDays: request.MeasurementPlan.BaselineWindowDays,
            runWindowDays: request.MeasurementPlan.RunWindowDays,
            guardrailMetricDefinitionIds: request.MeasurementPlan.GuardrailMetricDefinitionIds,
            minComplianceThreshold: request.MeasurementPlan.MinComplianceThreshold);

        // Create the experiment
        var experiment = Experiment.Create(
            userId: userId,
            title: request.Title,
            category: category,
            createdFrom: createdFrom,
            hypothesis: hypothesis,
            measurementPlan: measurementPlan,
            description: request.Description,
            linkedGoalIds: request.LinkedGoalIds,
            startDate: request.StartDate,
            endDatePlanned: request.EndDatePlanned);

        await _experimentRepository.AddAsync(experiment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return experiment.Id;
    }
}

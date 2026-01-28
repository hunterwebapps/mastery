using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;
using Mastery.Domain.ValueObjects;

namespace Mastery.Application.Features.Experiments.Commands.UpdateExperiment;

public sealed class UpdateExperimentCommandHandler : ICommandHandler<UpdateExperimentCommand>
{
    private readonly IExperimentRepository _experimentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateExperimentCommandHandler(
        IExperimentRepository experimentRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _experimentRepository = experimentRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateExperimentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var experiment = await _experimentRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Experiment.Experiment), request.Id);

        if (experiment.UserId != userId)
            throw new DomainException("Experiment does not belong to the current user.");

        // Parse optional enums
        ExperimentCategory? category = null;
        if (request.Category != null)
        {
            if (!Enum.TryParse<ExperimentCategory>(request.Category, out var parsedCategory))
                throw new DomainException($"Invalid experiment category: {request.Category}");
            category = parsedCategory;
        }

        // Build value objects if provided
        Hypothesis? hypothesis = null;
        if (request.Hypothesis != null)
        {
            hypothesis = Hypothesis.Create(
                change: request.Hypothesis.Change,
                expectedOutcome: request.Hypothesis.ExpectedOutcome,
                rationale: request.Hypothesis.Rationale);
        }

        MeasurementPlan? measurementPlan = null;
        if (request.MeasurementPlan != null)
        {
            if (!Enum.TryParse<MetricAggregation>(request.MeasurementPlan.PrimaryAggregation, out var aggregation))
                throw new DomainException($"Invalid metric aggregation: {request.MeasurementPlan.PrimaryAggregation}");

            measurementPlan = MeasurementPlan.Create(
                primaryMetricDefinitionId: request.MeasurementPlan.PrimaryMetricDefinitionId,
                primaryAggregation: aggregation,
                baselineWindowDays: request.MeasurementPlan.BaselineWindowDays,
                runWindowDays: request.MeasurementPlan.RunWindowDays,
                guardrailMetricDefinitionIds: request.MeasurementPlan.GuardrailMetricDefinitionIds,
                minComplianceThreshold: request.MeasurementPlan.MinComplianceThreshold);
        }

        experiment.Update(
            title: request.Title,
            description: request.Description,
            category: category,
            hypothesis: hypothesis,
            measurementPlan: measurementPlan,
            linkedGoalIds: request.LinkedGoalIds,
            startDate: request.StartDate,
            endDatePlanned: request.EndDatePlanned);

        await _experimentRepository.UpdateAsync(experiment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

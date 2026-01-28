using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Experiments.Models;
using Mastery.Domain.Entities.Experiment;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Experiments.Queries.GetActiveExperiment;

public sealed class GetActiveExperimentQueryHandler : IQueryHandler<GetActiveExperimentQuery, ExperimentDto?>
{
    private readonly IExperimentRepository _experimentRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetActiveExperimentQueryHandler(
        IExperimentRepository experimentRepository,
        ICurrentUserService currentUserService)
    {
        _experimentRepository = experimentRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ExperimentDto?> Handle(GetActiveExperimentQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return null;

        var experiment = await _experimentRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        if (experiment is null)
            return null;

        return MapToDto(experiment);
    }

    private static ExperimentDto MapToDto(Experiment experiment)
    {
        return new ExperimentDto
        {
            Id = experiment.Id,
            UserId = experiment.UserId,
            Title = experiment.Title,
            Description = experiment.Description,
            Category = experiment.Category.ToString(),
            Status = experiment.Status.ToString(),
            CreatedFrom = experiment.CreatedFrom.ToString(),
            Hypothesis = new HypothesisDto
            {
                Change = experiment.Hypothesis.Change,
                ExpectedOutcome = experiment.Hypothesis.ExpectedOutcome,
                Rationale = experiment.Hypothesis.Rationale,
                Summary = experiment.Hypothesis.ToString()
            },
            MeasurementPlan = new MeasurementPlanDto
            {
                PrimaryMetricDefinitionId = experiment.MeasurementPlan.PrimaryMetricDefinitionId,
                PrimaryAggregation = experiment.MeasurementPlan.PrimaryAggregation.ToString(),
                BaselineWindowDays = experiment.MeasurementPlan.BaselineWindowDays,
                RunWindowDays = experiment.MeasurementPlan.RunWindowDays,
                GuardrailMetricDefinitionIds = experiment.MeasurementPlan.GuardrailMetricDefinitionIds.ToList(),
                MinComplianceThreshold = experiment.MeasurementPlan.MinComplianceThreshold
            },
            StartDate = experiment.StartDate,
            EndDatePlanned = experiment.EndDatePlanned,
            EndDateActual = experiment.EndDateActual,
            LinkedGoalIds = experiment.LinkedGoalIds.ToList(),
            Notes = experiment.Notes
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new ExperimentNoteDto
                {
                    Id = n.Id,
                    Content = n.Content,
                    CreatedAt = n.CreatedAt
                })
                .ToList(),
            Result = experiment.Result != null ? new ExperimentResultDto
            {
                Id = experiment.Result.Id,
                BaselineValue = experiment.Result.BaselineValue,
                RunValue = experiment.Result.RunValue,
                Delta = experiment.Result.Delta,
                DeltaPercent = experiment.Result.DeltaPercent,
                OutcomeClassification = experiment.Result.OutcomeClassification.ToString(),
                ComplianceRate = experiment.Result.ComplianceRate,
                NarrativeSummary = experiment.Result.NarrativeSummary,
                ComputedAt = experiment.Result.ComputedAt
            } : null,
            DaysRemaining = experiment.DaysRemaining,
            DaysElapsed = experiment.DaysElapsed,
            CreatedAt = experiment.CreatedAt,
            ModifiedAt = experiment.ModifiedAt
        };
    }
}

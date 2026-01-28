using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Experiments.Models;
using Mastery.Domain.Entities.Experiment;
using Mastery.Domain.Enums;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Experiments.Queries.GetExperiments;

public sealed class GetExperimentsQueryHandler : IQueryHandler<GetExperimentsQuery, IReadOnlyList<ExperimentSummaryDto>>
{
    private readonly IExperimentRepository _experimentRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetExperimentsQueryHandler(
        IExperimentRepository experimentRepository,
        ICurrentUserService currentUserService)
    {
        _experimentRepository = experimentRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<ExperimentSummaryDto>> Handle(GetExperimentsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return [];

        IReadOnlyList<Experiment> experiments;

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<ExperimentStatus>(request.Status, out var status))
        {
            experiments = await _experimentRepository.GetByUserIdAndStatusAsync(userId, status, cancellationToken);
        }
        else
        {
            // Exclude archived experiments by default when no status filter is provided
            var allExperiments = await _experimentRepository.GetByUserIdAsync(userId, cancellationToken);
            experiments = allExperiments.Where(e => e.Status != ExperimentStatus.Archived).ToList();
        }

        return experiments.Select(MapToSummaryDto).ToList();
    }

    private static ExperimentSummaryDto MapToSummaryDto(Experiment experiment)
    {
        return new ExperimentSummaryDto
        {
            Id = experiment.Id,
            Title = experiment.Title,
            Category = experiment.Category.ToString(),
            Status = experiment.Status.ToString(),
            CreatedFrom = experiment.CreatedFrom.ToString(),
            HypothesisSummary = experiment.Hypothesis.ToString(),
            StartDate = experiment.StartDate,
            EndDatePlanned = experiment.EndDatePlanned,
            DaysRemaining = experiment.DaysRemaining,
            DaysElapsed = experiment.DaysElapsed,
            OutcomeClassification = experiment.Result?.OutcomeClassification.ToString(),
            NoteCount = experiment.Notes.Count,
            HasResult = experiment.Result != null,
            CreatedAt = experiment.CreatedAt
        };
    }
}

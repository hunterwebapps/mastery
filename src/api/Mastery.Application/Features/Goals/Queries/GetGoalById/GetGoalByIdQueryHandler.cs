using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Goals.Models;
using Mastery.Domain.Entities.Goal;
using Mastery.Domain.Entities.Metrics;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;

namespace Mastery.Application.Features.Goals.Queries.GetGoalById;

public sealed class GetGoalByIdQueryHandler : IQueryHandler<GetGoalByIdQuery, GoalDto>
{
    private readonly IGoalRepository _goalRepository;
    private readonly IMetricDefinitionRepository _metricDefinitionRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetGoalByIdQueryHandler(
        IGoalRepository goalRepository,
        IMetricDefinitionRepository metricDefinitionRepository,
        ICurrentUserService currentUserService)
    {
        _goalRepository = goalRepository;
        _metricDefinitionRepository = metricDefinitionRepository;
        _currentUserService = currentUserService;
    }

    public async Task<GoalDto> Handle(GetGoalByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var goal = await _goalRepository.GetByIdWithMetricsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Goal), request.Id);

        if (goal.UserId != userId)
            throw new DomainException("Goal does not belong to the current user.");

        // Get metric definitions for displaying names and units
        var metricDefIds = goal.Metrics.Select(m => m.MetricDefinitionId).Distinct().ToList();
        var metricDefs = await _metricDefinitionRepository.GetByIdsAsync(metricDefIds, cancellationToken);
        var metricDefDict = metricDefs.ToDictionary(m => m.Id);

        return MapToDto(goal, metricDefDict);
    }

    private static GoalDto MapToDto(Goal goal, Dictionary<Guid, MetricDefinition> metricDefs)
    {
        return new GoalDto
        {
            Id = goal.Id,
            UserId = goal.UserId,
            Title = goal.Title,
            Description = goal.Description,
            Why = goal.Why,
            Status = goal.Status.ToString(),
            Priority = goal.Priority,
            Deadline = goal.Deadline,
            SeasonId = goal.SeasonId,
            RoleIds = goal.RoleIds.ToList(),
            ValueIds = goal.ValueIds.ToList(),
            DependencyIds = goal.DependencyIds.ToList(),
            Metrics = goal.Metrics.Select(m => MapMetricToDto(m, metricDefs)).ToList(),
            CompletionNotes = goal.CompletionNotes,
            CompletedAt = goal.CompletedAt,
            CreatedAt = goal.CreatedAt,
            ModifiedAt = goal.ModifiedAt
        };
    }

    private static GoalMetricDto MapMetricToDto(GoalMetric metric, Dictionary<Guid, MetricDefinition> metricDefs)
    {
        var metricDef = metricDefs.GetValueOrDefault(metric.MetricDefinitionId);

        return new GoalMetricDto
        {
            Id = metric.Id,
            MetricDefinitionId = metric.MetricDefinitionId,
            MetricName = metricDef?.Name ?? "Unknown",
            Kind = metric.Kind.ToString(),
            Target = new TargetDto
            {
                Type = metric.Target.Type.ToString(),
                Value = metric.Target.Value,
                MaxValue = metric.Target.MaxValue
            },
            EvaluationWindow = new EvaluationWindowDto
            {
                WindowType = metric.EvaluationWindow.Type.ToString(),
                RollingDays = metric.EvaluationWindow.RollingDays,
                StartDay = metric.EvaluationWindow.StartDay.HasValue ? (int)metric.EvaluationWindow.StartDay.Value : null
            },
            Aggregation = metric.Aggregation.ToString(),
            Weight = metric.Weight,
            SourceHint = metric.SourceHint.ToString(),
            DisplayOrder = metric.DisplayOrder,
            Baseline = metric.Baseline,
            MinimumThreshold = metric.MinimumThreshold,
            Unit = metricDef?.Unit != null ? new MetricUnitDto
            {
                Type = metricDef.Unit.UnitType,
                Label = metricDef.Unit.DisplayLabel
            } : null
        };
    }
}

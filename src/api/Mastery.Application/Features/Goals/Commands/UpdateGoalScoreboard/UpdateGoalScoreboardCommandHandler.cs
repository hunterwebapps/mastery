using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Goal;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;
using Mastery.Domain.ValueObjects;

namespace Mastery.Application.Features.Goals.Commands.UpdateGoalScoreboard;

public sealed class UpdateGoalScoreboardCommandHandler : ICommandHandler<UpdateGoalScoreboardCommand>
{
    private readonly IGoalRepository _goalRepository;
    private readonly IMetricDefinitionRepository _metricDefinitionRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateGoalScoreboardCommandHandler(
        IGoalRepository goalRepository,
        IMetricDefinitionRepository metricDefinitionRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _goalRepository = goalRepository;
        _metricDefinitionRepository = metricDefinitionRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateGoalScoreboardCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var goal = await _goalRepository.GetByIdWithMetricsAsync(request.GoalId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Goal.Goal), request.GoalId);

        if (goal.UserId != userId)
            throw new DomainException("Goal does not belong to the current user.");

        // Validate all metric definitions exist and belong to user
        var metricDefIds = request.Metrics.Select(m => m.MetricDefinitionId).Distinct().ToList();
        var metricDefs = await _metricDefinitionRepository.GetByIdsAsync(metricDefIds, cancellationToken);

        foreach (var metricInput in request.Metrics)
        {
            var metricDef = metricDefs.FirstOrDefault(m => m.Id == metricInput.MetricDefinitionId)
                ?? throw new NotFoundException(nameof(Domain.Entities.Metrics.MetricDefinition), metricInput.MetricDefinitionId);

            if (metricDef.UserId != userId)
                throw new DomainException($"Metric definition {metricInput.MetricDefinitionId} does not belong to the current user.");
        }

        // Build new metrics list
        var newMetrics = new List<GoalMetric>();

        foreach (var metricInput in request.Metrics)
        {
            // Parse enums
            if (!Enum.TryParse<MetricKind>(metricInput.Kind, out var kind))
                throw new DomainException($"Invalid metric kind: {metricInput.Kind}");

            if (!Enum.TryParse<MetricAggregation>(metricInput.Aggregation, out var aggregation))
                throw new DomainException($"Invalid aggregation: {metricInput.Aggregation}");

            if (!Enum.TryParse<MetricSourceType>(metricInput.SourceHint, out var sourceHint))
                throw new DomainException($"Invalid source hint: {metricInput.SourceHint}");

            // Create value objects
            var target = CreateTarget(metricInput.Target);
            var window = CreateEvaluationWindow(metricInput.EvaluationWindow);

            var metric = GoalMetric.Create(
                goalId: goal.Id,
                metricDefinitionId: metricInput.MetricDefinitionId,
                kind: kind,
                target: target,
                evaluationWindow: window,
                aggregation: aggregation,
                weight: metricInput.Weight,
                sourceHint: sourceHint,
                displayOrder: metricInput.DisplayOrder,
                baseline: metricInput.Baseline,
                minimumThreshold: metricInput.MinimumThreshold);

            newMetrics.Add(metric);
        }

        // Replace all metrics
        goal.UpdateMetrics(newMetrics);

        await _goalRepository.UpdateAsync(goal, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static Target CreateTarget(CreateGoal.CreateTargetInput input)
    {
        if (!Enum.TryParse<TargetType>(input.Type, out var targetType))
            throw new DomainException($"Invalid target type: {input.Type}");

        return targetType switch
        {
            TargetType.Between => Target.Between(input.Value, input.MaxValue
                ?? throw new DomainException("MaxValue is required for Between target type.")),
            _ => Target.Create(targetType, input.Value)
        };
    }

    private static EvaluationWindow CreateEvaluationWindow(CreateGoal.CreateEvaluationWindowInput input)
    {
        if (!Enum.TryParse<WindowType>(input.WindowType, out var windowType))
            throw new DomainException($"Invalid window type: {input.WindowType}");

        return windowType switch
        {
            WindowType.Rolling => EvaluationWindow.Rolling(input.RollingDays
                ?? throw new DomainException("RollingDays is required for Rolling window type.")),
            WindowType.Weekly => EvaluationWindow.Weekly(input.StartDay.HasValue ? (DayOfWeek)input.StartDay.Value : DayOfWeek.Monday),
            WindowType.Monthly => EvaluationWindow.Monthly(),
            _ => EvaluationWindow.Daily()
        };
    }
}

using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Goal;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;
using Mastery.Domain.ValueObjects;

namespace Mastery.Application.Features.Goals.Commands.CreateGoal;

public sealed class CreateGoalCommandHandler : ICommandHandler<CreateGoalCommand, Guid>
{
    private readonly IGoalRepository _goalRepository;
    private readonly IMetricDefinitionRepository _metricDefinitionRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateGoalCommandHandler(
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

    public async Task<Guid> Handle(CreateGoalCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        // Create the goal
        var goal = Goal.Create(
            userId: userId,
            title: request.Title,
            description: request.Description,
            why: request.Why,
            priority: request.Priority,
            deadline: request.Deadline,
            seasonId: request.SeasonId,
            roleIds: request.RoleIds,
            valueIds: request.ValueIds,
            dependencyIds: request.DependencyIds);

        // Add metrics if provided
        if (request.Metrics?.Count > 0)
        {
            // Validate all metric definitions exist and belong to user
            var metricDefIds = request.Metrics.Select(m => m.MetricDefinitionId).Distinct().ToList();
            var metricDefs = await _metricDefinitionRepository.GetByIdsAsync(metricDefIds, cancellationToken);

            foreach (var metricInput in request.Metrics)
            {
                var metricDef = metricDefs.FirstOrDefault(m => m.Id == metricInput.MetricDefinitionId)
                    ?? throw new NotFoundException(nameof(Domain.Entities.Metrics.MetricDefinition), metricInput.MetricDefinitionId);

                if (metricDef.UserId != userId)
                    throw new DomainException($"Metric definition {metricInput.MetricDefinitionId} does not belong to the current user.");

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

                goal.AddMetric(
                    metricDefinitionId: metricInput.MetricDefinitionId,
                    kind: kind,
                    target: target,
                    evaluationWindow: window,
                    aggregation: aggregation,
                    weight: metricInput.Weight,
                    sourceHint: sourceHint,
                    baseline: metricInput.Baseline,
                    minimumThreshold: metricInput.MinimumThreshold);
            }
        }

        await _goalRepository.AddAsync(goal, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return goal.Id;
    }

    private static Target CreateTarget(CreateTargetInput input)
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

    private static EvaluationWindow CreateEvaluationWindow(CreateEvaluationWindowInput input)
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

using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Entities.Metrics;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.Interfaces;
using Mastery.Domain.ValueObjects;

namespace Mastery.Application.Features.Goals.Commands.AddGoalMetric;

public sealed class AddGoalMetricCommandHandler : ICommandHandler<AddGoalMetricCommand, AddGoalMetricResult>
{
    private readonly IGoalRepository _goalRepository;
    private readonly IMetricDefinitionRepository _metricDefinitionRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public AddGoalMetricCommandHandler(
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

    public async Task<AddGoalMetricResult> Handle(AddGoalMetricCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new DomainException("User not authenticated.");

        var createdNewDefinition = false;
        Guid metricDefId;

        // ═══════════════════════════════════════════════════════════════
        // STEP 1: RESOLVE OR CREATE MetricDefinition
        // This MUST happen first because GoalMetric requires a MetricDefinitionId
        // ═══════════════════════════════════════════════════════════════

        if (request.ExistingMetricDefinitionId.HasValue)
        {
            // SCENARIO A: Use existing MetricDefinition
            var existing = await _metricDefinitionRepository.GetByIdAsync(
                request.ExistingMetricDefinitionId.Value, cancellationToken);

            if (existing is null || existing.UserId != userId)
                throw new NotFoundException(nameof(MetricDefinition), request.ExistingMetricDefinitionId.Value);

            metricDefId = existing.Id;
        }
        else if (!string.IsNullOrWhiteSpace(request.NewMetricName))
        {
            // SCENARIO B: CREATE NEW MetricDefinition FIRST
            if (await _metricDefinitionRepository.ExistsByUserIdAndNameAsync(userId, request.NewMetricName, cancellationToken))
                throw new DomainException($"Metric '{request.NewMetricName}' already exists.");

            var dataType = Enum.TryParse<MetricDataType>(request.NewMetricDataType, out var dt)
                ? dt : MetricDataType.Number;
            var direction = Enum.TryParse<MetricDirection>(request.NewMetricDirection, out var dir)
                ? dir : MetricDirection.Increase;

            var metricDef = MetricDefinition.Create(
                userId: userId,
                name: request.NewMetricName,
                description: request.NewMetricDescription,
                dataType: dataType,
                unit: MetricUnit.None,
                direction: direction,
                defaultCadence: WindowType.Daily,
                defaultAggregation: MetricAggregation.Sum);

            await _metricDefinitionRepository.AddAsync(metricDef, cancellationToken);
            metricDefId = metricDef.Id;
            createdNewDefinition = true;
        }
        else
        {
            throw new DomainException("Must provide either existingMetricDefinitionId or newMetricName.");
        }

        // ═══════════════════════════════════════════════════════════════
        // STEP 2: Load goal with metrics (tracked by EF Core)
        // ═══════════════════════════════════════════════════════════════

        var goal = await _goalRepository.GetByIdWithMetricsAsync(request.GoalId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Goal.Goal), request.GoalId);

        if (goal.UserId != userId)
            throw new DomainException("Goal does not belong to the current user.");

        // ═══════════════════════════════════════════════════════════════
        // STEP 3: Build value objects
        // ═══════════════════════════════════════════════════════════════

        if (!Enum.TryParse<MetricKind>(request.Kind, out var kind))
            throw new DomainException($"Invalid metric kind: {request.Kind}");

        var target = CreateTarget(request.TargetType, request.TargetValue, request.TargetMaxValue);
        var window = CreateEvaluationWindow(request.WindowType, request.RollingDays, request.WeekStartDay);

        var aggregation = Enum.TryParse<MetricAggregation>(request.Aggregation, out var agg)
            ? agg : MetricAggregation.Sum;
        var sourceHint = Enum.TryParse<MetricSourceType>(request.SourceHint, out var src)
            ? src : MetricSourceType.Manual;

        // ═══════════════════════════════════════════════════════════════
        // STEP 4: CREATE THE GoalMetric using the MetricDefinitionId
        // ═══════════════════════════════════════════════════════════════

        var goalMetric = goal.AddMetric(
            metricDefinitionId: metricDefId,
            kind: kind,
            target: target,
            evaluationWindow: window,
            aggregation: aggregation,
            weight: request.Weight,
            sourceHint: sourceHint,
            baseline: request.Baseline,
            minimumThreshold: request.MinimumThreshold);

        // Explicitly add the GoalMetric to the DbContext to ensure it's tracked
        // This is needed because adding to a backing field collection may not
        // trigger EF Core's change detection automatically
        await _goalRepository.AddGoalMetricAsync(goalMetric, cancellationToken);

        // ═══════════════════════════════════════════════════════════════
        // STEP 5: Persist
        // ═══════════════════════════════════════════════════════════════

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AddGoalMetricResult(goal.Id, metricDefId, goalMetric.Id, createdNewDefinition);
    }

    private static Target CreateTarget(string type, decimal value, decimal? maxValue)
    {
        if (!Enum.TryParse<TargetType>(type, out var targetType))
            throw new DomainException($"Invalid target type: {type}");

        return targetType switch
        {
            TargetType.Between => Target.Between(value, maxValue
                ?? throw new DomainException("MaxValue is required for Between target type.")),
            _ => Target.Create(targetType, value)
        };
    }

    private static EvaluationWindow CreateEvaluationWindow(string windowType, int? rollingDays, int? weekStartDay)
    {
        if (!Enum.TryParse<WindowType>(windowType, out var wt))
            wt = WindowType.Weekly;

        return wt switch
        {
            WindowType.Rolling => EvaluationWindow.Rolling(rollingDays
                ?? throw new DomainException("RollingDays is required for Rolling window type.")),
            WindowType.Weekly => EvaluationWindow.Weekly(weekStartDay.HasValue ? (DayOfWeek)weekStartDay.Value : DayOfWeek.Monday),
            WindowType.Monthly => EvaluationWindow.Monthly(),
            _ => EvaluationWindow.Daily()
        };
    }
}

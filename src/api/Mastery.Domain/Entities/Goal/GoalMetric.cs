using Mastery.Domain.Common;
using Mastery.Domain.Enums;
using Mastery.Domain.Exceptions;
using Mastery.Domain.ValueObjects;

namespace Mastery.Domain.Entities.Goal;

/// <summary>
/// Represents a metric attached to a goal's scoreboard.
/// This is how a MetricDefinition is used within a specific goal context.
/// </summary>
public sealed class GoalMetric : AuditableEntity
{
    /// <summary>
    /// The goal this metric belongs to.
    /// </summary>
    public Guid GoalId { get; private set; }

    /// <summary>
    /// Reference to the metric definition (what is being measured).
    /// </summary>
    public Guid MetricDefinitionId { get; private set; }

    /// <summary>
    /// The role this metric plays in the goal's scoreboard.
    /// </summary>
    public MetricKind Kind { get; private set; }

    /// <summary>
    /// The target configuration for this metric within this goal.
    /// </summary>
    public Target Target { get; private set; } = null!;

    /// <summary>
    /// The evaluation window for assessing target achievement.
    /// </summary>
    public EvaluationWindow EvaluationWindow { get; private set; } = null!;

    /// <summary>
    /// How observations are aggregated for this metric in this goal.
    /// </summary>
    public MetricAggregation Aggregation { get; private set; }

    /// <summary>
    /// Weight of this metric in goal health calculations (0.0 to 1.0).
    /// </summary>
    public decimal Weight { get; private set; } = 1.0m;

    /// <summary>
    /// Hint about the expected observation source for this metric.
    /// </summary>
    public MetricSourceType SourceHint { get; private set; }

    /// <summary>
    /// Display order within the scoreboard.
    /// </summary>
    [EmbeddingIgnore]
    public int DisplayOrder { get; private set; }

    /// <summary>
    /// Optional baseline value for progress calculations.
    /// </summary>
    public decimal? Baseline { get; private set; }

    /// <summary>
    /// Optional minimum acceptable threshold to trigger drift warnings.
    /// </summary>
    public decimal? MinimumThreshold { get; private set; }

    private GoalMetric() { } // EF Core

    public static GoalMetric Create(
        Guid goalId,
        Guid metricDefinitionId,
        MetricKind kind,
        Target target,
        EvaluationWindow evaluationWindow,
        MetricAggregation aggregation,
        decimal weight = 1.0m,
        MetricSourceType sourceHint = MetricSourceType.Manual,
        int displayOrder = 0,
        decimal? baseline = null,
        decimal? minimumThreshold = null)
    {
        if (goalId == Guid.Empty)
            throw new DomainException("GoalId cannot be empty.");

        if (metricDefinitionId == Guid.Empty)
            throw new DomainException("MetricDefinitionId cannot be empty.");

        if (weight < 0 || weight > 1)
            throw new DomainException("Weight must be between 0 and 1.");

        return new GoalMetric
        {
            GoalId = goalId,
            MetricDefinitionId = metricDefinitionId,
            Kind = kind,
            Target = target,
            EvaluationWindow = evaluationWindow,
            Aggregation = aggregation,
            Weight = weight,
            SourceHint = sourceHint,
            DisplayOrder = displayOrder,
            Baseline = baseline,
            MinimumThreshold = minimumThreshold
        };
    }

    public void UpdateTarget(Target target)
    {
        Target = target ?? throw new DomainException("Target cannot be null.");
    }

    public void UpdateEvaluationWindow(EvaluationWindow window)
    {
        EvaluationWindow = window ?? throw new DomainException("Evaluation window cannot be null.");
    }

    public void UpdateAggregation(MetricAggregation aggregation)
    {
        Aggregation = aggregation;
    }

    public void UpdateWeight(decimal weight)
    {
        if (weight < 0 || weight > 1)
            throw new DomainException("Weight must be between 0 and 1.");
        Weight = weight;
    }

    public void UpdateDisplayOrder(int order)
    {
        DisplayOrder = order;
    }

    public void UpdateBaseline(decimal? baseline)
    {
        Baseline = baseline;
    }

    public void UpdateMinimumThreshold(decimal? threshold)
    {
        MinimumThreshold = threshold;
    }

    public void UpdateSourceHint(MetricSourceType sourceHint)
    {
        SourceHint = sourceHint;
    }
}

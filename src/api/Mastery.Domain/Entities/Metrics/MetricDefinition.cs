using Mastery.Domain.Common;
using Mastery.Domain.Enums;
using Mastery.Domain.Events;
using Mastery.Domain.Exceptions;
using Mastery.Domain.ValueObjects;

namespace Mastery.Domain.Entities.Metrics;

/// <summary>
/// Represents a reusable metric definition in the user's metric library.
/// This is the "what is measured" - separate from how it's used in any specific goal.
/// </summary>
public sealed class MetricDefinition : OwnedEntity, IAggregateRoot
{
    /// <summary>
    /// Display name of the metric (e.g., "Deep Work Minutes", "Body Weight").
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Optional description of what this metric measures.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// The data type of values for this metric.
    /// </summary>
    public MetricDataType DataType { get; private set; }

    /// <summary>
    /// The unit of measurement.
    /// </summary>
    public MetricUnit Unit { get; private set; } = null!;

    /// <summary>
    /// The desired direction of change.
    /// </summary>
    public MetricDirection Direction { get; private set; }

    /// <summary>
    /// Default cadence for observations (can be overridden per goal).
    /// </summary>
    public WindowType DefaultCadence { get; private set; }

    /// <summary>
    /// Default aggregation method (can be overridden per goal).
    /// </summary>
    public MetricAggregation DefaultAggregation { get; private set; }

    /// <summary>
    /// Whether this metric is archived (hidden from library but history remains).
    /// </summary>
    public bool IsArchived { get; private set; }

    /// <summary>
    /// Optional tags for categorization.
    /// </summary>
    private List<string> _tags = [];
    public IReadOnlyList<string> Tags => _tags.AsReadOnly();

    private MetricDefinition() { } // EF Core

    public static MetricDefinition Create(
        string userId,
        string name,
        MetricDataType dataType,
        MetricUnit unit,
        MetricDirection direction,
        string? description = null,
        WindowType defaultCadence = WindowType.Daily,
        MetricAggregation defaultAggregation = MetricAggregation.Sum,
        IEnumerable<string>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("UserId cannot be empty.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Metric name cannot be empty.");

        if (name.Length > 100)
            throw new DomainException("Metric name cannot exceed 100 characters.");

        var metric = new MetricDefinition
        {
            UserId = userId,
            Name = name,
            Description = description,
            DataType = dataType,
            Unit = unit,
            Direction = direction,
            DefaultCadence = defaultCadence,
            DefaultAggregation = defaultAggregation,
            IsArchived = false,
            _tags = tags?.ToList() ?? []
        };

        metric.AddDomainEvent(new MetricDefinitionCreatedEvent(metric.Id, userId, name));

        return metric;
    }

    public void Update(
        string? name = null,
        string? description = null,
        MetricUnit? unit = null,
        MetricDirection? direction = null,
        WindowType? defaultCadence = null,
        MetricAggregation? defaultAggregation = null,
        IEnumerable<string>? tags = null)
    {
        if (name != null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Metric name cannot be empty.");
            if (name.Length > 100)
                throw new DomainException("Metric name cannot exceed 100 characters.");
            Name = name;
        }

        if (description != null)
            Description = description;

        if (unit != null)
            Unit = unit;

        if (direction.HasValue)
            Direction = direction.Value;

        if (defaultCadence.HasValue)
            DefaultCadence = defaultCadence.Value;

        if (defaultAggregation.HasValue)
            DefaultAggregation = defaultAggregation.Value;

        if (tags != null)
            _tags = tags.ToList();

        AddDomainEvent(new MetricDefinitionUpdatedEvent(Id, Name));
    }

    public void Archive()
    {
        if (IsArchived)
            throw new DomainException("Metric is already archived.");

        IsArchived = true;
        AddDomainEvent(new MetricDefinitionArchivedEvent(Id, UserId));
    }

    public void Unarchive()
    {
        if (!IsArchived)
            throw new DomainException("Metric is not archived.");

        IsArchived = false;
    }
}

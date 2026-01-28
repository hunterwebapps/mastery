using Mastery.Domain.Common;
using Mastery.Domain.Enums;
using Mastery.Domain.Events;
using Mastery.Domain.Exceptions;

namespace Mastery.Domain.Entities.Metrics;

/// <summary>
/// Represents a single observation/measurement for a metric at a point in time.
/// This is append-only; corrections create new observations with correction references.
/// </summary>
public sealed class MetricObservation : BaseEntity
{
    /// <summary>
    /// The metric definition this observation belongs to.
    /// </summary>
    public Guid MetricDefinitionId { get; private set; }

    /// <summary>
    /// The user who owns this observation.
    /// </summary>
    public string UserId { get; private set; } = null!;

    /// <summary>
    /// The exact timestamp when the observation was recorded (UTC).
    /// </summary>
    public DateTime ObservedAt { get; private set; }

    /// <summary>
    /// The date of the observation (denormalized for efficient date-range queries).
    /// This is in the user's timezone.
    /// </summary>
    public DateOnly ObservedOn { get; private set; }

    /// <summary>
    /// The numeric value of the observation.
    /// For booleans: 1 = true, 0 = false.
    /// For ratings: 1-5.
    /// </summary>
    public decimal Value { get; private set; }

    /// <summary>
    /// The source of this observation.
    /// </summary>
    public MetricSourceType Source { get; private set; }

    /// <summary>
    /// Optional correlation ID linking to the source event (e.g., habit completion ID).
    /// </summary>
    public string? CorrelationId { get; private set; }

    /// <summary>
    /// Optional note about this observation.
    /// </summary>
    public string? Note { get; private set; }

    /// <summary>
    /// When this observation was created (system timestamp).
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// If this is a correction, the ID of the observation being corrected.
    /// </summary>
    public Guid? CorrectedObservationId { get; private set; }

    /// <summary>
    /// Whether this observation has been superseded by a correction.
    /// </summary>
    public bool IsCorrected { get; private set; }

    private MetricObservation() { } // EF Core

    public static MetricObservation Create(
        Guid metricDefinitionId,
        string userId,
        DateTime observedAt,
        DateOnly observedOn,
        decimal value,
        MetricSourceType source,
        string? correlationId = null,
        string? note = null)
    {
        if (metricDefinitionId == Guid.Empty)
            throw new DomainException("MetricDefinitionId cannot be empty.");

        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("UserId cannot be empty.");

        var observation = new MetricObservation
        {
            MetricDefinitionId = metricDefinitionId,
            UserId = userId,
            ObservedAt = observedAt,
            ObservedOn = observedOn,
            Value = value,
            Source = source,
            CorrelationId = correlationId,
            Note = note,
            CreatedAt = DateTime.UtcNow,
            IsCorrected = false
        };

        observation.AddDomainEvent(new MetricObservationRecordedEvent(
            observation.Id,
            metricDefinitionId,
            userId,
            observedOn,
            value,
            source));

        return observation;
    }

    /// <summary>
    /// Creates a correction observation that supersedes this one.
    /// </summary>
    public MetricObservation CreateCorrection(
        decimal newValue,
        string? note = null)
    {
        if (IsCorrected)
            throw new DomainException("This observation has already been corrected.");

        // Mark this observation as corrected
        IsCorrected = true;

        // Create the correction observation
        var correction = new MetricObservation
        {
            MetricDefinitionId = MetricDefinitionId,
            UserId = UserId,
            ObservedAt = ObservedAt,
            ObservedOn = ObservedOn,
            Value = newValue,
            Source = MetricSourceType.Manual, // Corrections are always manual
            CorrelationId = CorrelationId,
            Note = note ?? $"Correction of value from {Value} to {newValue}",
            CreatedAt = DateTime.UtcNow,
            CorrectedObservationId = Id,
            IsCorrected = false
        };

        correction.AddDomainEvent(new MetricObservationCorrectedEvent(
            correction.Id,
            Id,
            MetricDefinitionId,
            UserId,
            Value,
            newValue));

        return correction;
    }
}

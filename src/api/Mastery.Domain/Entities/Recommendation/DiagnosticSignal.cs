using Mastery.Domain.Common;
using Mastery.Domain.Enums;
using Mastery.Domain.Events;
using Mastery.Domain.Exceptions;
using Mastery.Domain.ValueObjects;

namespace Mastery.Domain.Entities.Recommendation;

/// <summary>
/// A diagnostic signal detected by the signal detection pipeline.
/// Signals exist independently of recommendations and may be referenced by multiple.
/// Aggregate root.
/// </summary>
public sealed class DiagnosticSignal : AuditableEntity, IAggregateRoot
{
    public string UserId { get; private set; } = null!;
    public SignalType Type { get; private set; }
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public int Severity { get; private set; }
    public SignalEvidence Evidence { get; private set; } = null!;
    public DateOnly DetectedOn { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? ResolvedByRecommendationId { get; private set; }

    private DiagnosticSignal() { } // EF Core

    public static DiagnosticSignal Create(
        string userId,
        SignalType type,
        string title,
        string description,
        int severity,
        SignalEvidence evidence,
        DateOnly detectedOn)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("UserId cannot be empty.");

        if (severity < 0 || severity > 100)
            throw new DomainException("Severity must be between 0 and 100.");

        var signal = new DiagnosticSignal
        {
            UserId = userId,
            Type = type,
            Title = title,
            Description = description,
            Severity = severity,
            Evidence = evidence,
            DetectedOn = detectedOn,
            IsActive = true
        };

        signal.AddDomainEvent(new DiagnosticSignalDetectedEvent(signal.Id, userId, type, severity));

        return signal;
    }

    public void Resolve(Guid recommendationId)
    {
        IsActive = false;
        ResolvedByRecommendationId = recommendationId;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}

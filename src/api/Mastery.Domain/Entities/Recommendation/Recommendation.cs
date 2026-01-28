using Mastery.Domain.Common;
using Mastery.Domain.Enums;
using Mastery.Domain.Events;
using Mastery.Domain.Exceptions;
using Mastery.Domain.ValueObjects;

namespace Mastery.Domain.Entities.Recommendation;

/// <summary>
/// A typed, executable recommendation produced by the recommendation pipeline.
/// Aggregate root.
/// </summary>
public sealed class Recommendation : AuditableEntity, IAggregateRoot
{
    public string UserId { get; private set; } = null!;
    public RecommendationType Type { get; private set; }
    public RecommendationStatus Status { get; private set; }
    public RecommendationContext Context { get; private set; }
    public RecommendationTarget Target { get; private set; } = null!;
    public RecommendationActionKind ActionKind { get; private set; }
    public string Title { get; private set; } = null!;
    public string Rationale { get; private set; } = null!;
    public string? ActionPayload { get; private set; }
    public decimal Score { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? RespondedAt { get; private set; }
    public string? DismissReason { get; private set; }

    private List<Guid> _signalIds = [];
    public IReadOnlyList<Guid> SignalIds => _signalIds.AsReadOnly();

    public RecommendationTrace? Trace { get; private set; }

    private Recommendation() { } // EF Core

    public static Recommendation Create(
        string userId,
        RecommendationType type,
        RecommendationContext context,
        RecommendationTarget target,
        RecommendationActionKind actionKind,
        string title,
        string rationale,
        decimal score,
        string? actionPayload = null,
        DateTime? expiresAt = null,
        IEnumerable<Guid>? signalIds = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("UserId cannot be empty.");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Recommendation title cannot be empty.");

        if (string.IsNullOrWhiteSpace(rationale))
            throw new DomainException("Recommendation rationale cannot be empty.");

        return new Recommendation
        {
            UserId = userId,
            Type = type,
            Status = RecommendationStatus.Pending,
            Context = context,
            Target = target,
            ActionKind = actionKind,
            Title = title,
            Rationale = rationale,
            Score = score,
            ActionPayload = actionPayload,
            ExpiresAt = expiresAt,
            _signalIds = signalIds?.ToList() ?? []
        };
    }

    public void Accept()
    {
        if (Status != RecommendationStatus.Pending && Status != RecommendationStatus.Snoozed)
            throw new DomainException($"Cannot accept a recommendation with status {Status}.");

        Status = RecommendationStatus.Accepted;
        RespondedAt = DateTime.UtcNow;

        AddDomainEvent(new RecommendationAcceptedEvent(Id, UserId, Type));
    }

    public void Dismiss(string? reason = null)
    {
        if (Status != RecommendationStatus.Pending && Status != RecommendationStatus.Snoozed)
            throw new DomainException($"Cannot dismiss a recommendation with status {Status}.");

        Status = RecommendationStatus.Dismissed;
        DismissReason = reason;
        RespondedAt = DateTime.UtcNow;

        AddDomainEvent(new RecommendationDismissedEvent(Id, UserId, Type, reason));
    }

    public void Snooze()
    {
        if (Status != RecommendationStatus.Pending)
            throw new DomainException($"Cannot snooze a recommendation with status {Status}.");

        Status = RecommendationStatus.Snoozed;
        RespondedAt = DateTime.UtcNow;

        AddDomainEvent(new RecommendationSnoozedEvent(Id, UserId));
    }

    public void MarkExpired()
    {
        if (Status != RecommendationStatus.Pending && Status != RecommendationStatus.Snoozed)
            return; // Silently skip if already resolved

        Status = RecommendationStatus.Expired;
    }

    public void MarkExecuted()
    {
        if (Status != RecommendationStatus.Accepted)
            throw new DomainException("Only accepted recommendations can be marked as executed.");

        Status = RecommendationStatus.Executed;
    }

    public void AttachTrace(RecommendationTrace trace)
    {
        Trace = trace ?? throw new DomainException("Trace cannot be null.");
    }
}

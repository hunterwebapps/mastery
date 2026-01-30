using Mastery.Domain.Enums;

namespace Mastery.Domain.Entities.Signal;

/// <summary>
/// Represents a domain event signal that needs to be processed for recommendation generation.
/// Signals are classified by priority and scheduled for processing at appropriate windows.
/// </summary>
public sealed class SignalEntry
{
    /// <summary>
    /// Auto-incrementing primary key (BIGINT IDENTITY).
    /// </summary>
    public long Id { get; private set; }

    /// <summary>
    /// The user ID this signal belongs to.
    /// </summary>
    public string UserId { get; private set; } = null!;

    /// <summary>
    /// The type of domain event that triggered this signal (e.g., "CheckInSubmitted", "HabitCompleted").
    /// </summary>
    public string EventType { get; private set; } = null!;

    /// <summary>
    /// JSON-serialized event data for context during processing.
    /// </summary>
    public string? EventDataJson { get; private set; }

    /// <summary>
    /// Priority level determining processing urgency.
    /// </summary>
    public SignalPriority Priority { get; private set; }

    /// <summary>
    /// The type of processing window for this signal.
    /// </summary>
    public ProcessingWindowType WindowType { get; private set; }

    /// <summary>
    /// The scheduled start time of the processing window (UTC).
    /// Null for Immediate window type.
    /// </summary>
    public DateTime? ScheduledWindowStart { get; private set; }

    /// <summary>
    /// The entity type that triggered this signal (e.g., "Goal", "Habit", "CheckIn").
    /// </summary>
    public string? TargetEntityType { get; private set; }

    /// <summary>
    /// The ID of the entity that triggered this signal.
    /// </summary>
    public Guid? TargetEntityId { get; private set; }

    /// <summary>
    /// Current status of this signal.
    /// </summary>
    public SignalStatus Status { get; private set; }

    /// <summary>
    /// When this signal was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When this signal was processed (successfully or skipped).
    /// </summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>
    /// Lease expiration time. If set and in the future, another worker has this signal.
    /// </summary>
    public DateTime? LeasedUntil { get; private set; }

    /// <summary>
    /// Identifier of the worker that currently holds the lease.
    /// </summary>
    public string? LeaseHolder { get; private set; }

    /// <summary>
    /// Number of times processing has been attempted.
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// Error message from the last failed processing attempt.
    /// </summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// The tier at which this signal was processed (Tier0/Tier1/Tier2/Skipped).
    /// </summary>
    public AssessmentTier? ProcessingTier { get; private set; }

    /// <summary>
    /// Reason why the signal was skipped (if status is Skipped).
    /// </summary>
    public string? SkipReason { get; private set; }

    /// <summary>
    /// Expiration time for this signal. Signals older than this are marked Expired.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// Number of times this signal has been deferred due to pending embeddings.
    /// </summary>
    public int DeferralCount { get; private set; }

    /// <summary>
    /// If set, the signal should not be processed until after this time.
    /// Used for deferral when embeddings are still being generated.
    /// </summary>
    public DateTime? NextProcessAfter { get; private set; }

    // Private constructor for EF Core
    private SignalEntry() { }

    /// <summary>
    /// Creates a new signal entry.
    /// </summary>
    public static SignalEntry Create(
        string userId,
        string eventType,
        SignalPriority priority,
        ProcessingWindowType windowType,
        DateTime createdAt,
        string? eventDataJson = null,
        string? targetEntityType = null,
        Guid? targetEntityId = null,
        DateTime? scheduledWindowStart = null,
        TimeSpan? ttl = null)
    {
        var defaultTtl = priority switch
        {
            SignalPriority.Urgent => TimeSpan.FromHours(1),
            SignalPriority.WindowAligned => TimeSpan.FromHours(24),
            SignalPriority.Standard => TimeSpan.FromHours(48),
            SignalPriority.Low => TimeSpan.FromHours(72),
            _ => TimeSpan.FromHours(48)
        };

        return new SignalEntry
        {
            UserId = userId,
            EventType = eventType,
            EventDataJson = eventDataJson,
            Priority = priority,
            WindowType = windowType,
            ScheduledWindowStart = scheduledWindowStart,
            TargetEntityType = targetEntityType,
            TargetEntityId = targetEntityId,
            Status = SignalStatus.Pending,
            CreatedAt = createdAt,
            RetryCount = 0,
            ExpiresAt = createdAt.Add(ttl ?? defaultTtl)
        };
    }

    /// <summary>
    /// Attempts to acquire a lease on this signal.
    /// Returns true if the lease was acquired, false if already leased or not pending.
    /// </summary>
    public bool TryAcquireLease(string leaseHolder, DateTime leasedUntil, DateTime now)
    {
        // Can only lease if Pending, or if Processing with expired lease
        if (Status == SignalStatus.Pending ||
            (Status == SignalStatus.Processing && LeasedUntil.HasValue && LeasedUntil.Value < now))
        {
            LeaseHolder = leaseHolder;
            LeasedUntil = leasedUntil;
            Status = SignalStatus.Processing;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Marks this signal as successfully processed.
    /// </summary>
    public void MarkProcessed(DateTime processedAt, AssessmentTier tier)
    {
        ProcessedAt = processedAt;
        ProcessingTier = tier;
        Status = SignalStatus.Processed;
        LeasedUntil = null;
        LeaseHolder = null;
    }

    /// <summary>
    /// Marks this signal as skipped (evaluated but no action needed).
    /// </summary>
    public void MarkSkipped(DateTime processedAt, string reason)
    {
        ProcessedAt = processedAt;
        ProcessingTier = AssessmentTier.Skipped;
        SkipReason = reason;
        Status = SignalStatus.Skipped;
        LeasedUntil = null;
        LeaseHolder = null;
    }

    /// <summary>
    /// Marks this signal as failed, incrementing the retry count.
    /// </summary>
    public void MarkFailed(string error, int maxRetries)
    {
        RetryCount++;
        LastError = error?.Length > 500 ? error[..500] : error;
        LeasedUntil = null;
        LeaseHolder = null;

        Status = RetryCount >= maxRetries
            ? SignalStatus.Failed
            : SignalStatus.Pending;
    }

    /// <summary>
    /// Marks this signal as expired.
    /// </summary>
    public void MarkExpired()
    {
        Status = SignalStatus.Expired;
        LeasedUntil = null;
        LeaseHolder = null;
    }

    /// <summary>
    /// Releases the lease without marking as processed or failed.
    /// </summary>
    public void ReleaseLease()
    {
        if (Status == SignalStatus.Processing)
        {
            Status = SignalStatus.Pending;
            LeasedUntil = null;
            LeaseHolder = null;
        }
    }

    /// <summary>
    /// Defers processing of this signal until the specified time.
    /// Increments the deferral count and releases the lease.
    /// </summary>
    /// <param name="deferUntil">When the signal can be processed next.</param>
    public void Defer(DateTime deferUntil)
    {
        DeferralCount++;
        NextProcessAfter = deferUntil;
        ReleaseLease();
    }

    /// <summary>
    /// Returns true if the signal has reached the maximum deferral count.
    /// </summary>
    public bool HasReachedMaxDeferrals(int maxDeferrals) => DeferralCount >= maxDeferrals;

    /// <summary>
    /// Checks if this signal has expired.
    /// </summary>
    public bool IsExpired(DateTime now) => now > ExpiresAt;

    /// <summary>
    /// Checks if this signal is ready for processing in its scheduled window.
    /// </summary>
    public bool IsReadyForWindow(DateTime now)
    {
        if (Status != SignalStatus.Pending)
            return false;

        if (WindowType == ProcessingWindowType.Immediate)
            return true;

        return ScheduledWindowStart.HasValue && now >= ScheduledWindowStart.Value;
    }
}

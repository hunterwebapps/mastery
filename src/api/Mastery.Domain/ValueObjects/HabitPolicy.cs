using System.Text.Json.Serialization;
using Mastery.Domain.Common;
using Mastery.Domain.Exceptions;

namespace Mastery.Domain.ValueObjects;

/// <summary>
/// Defines the behavioral rules for a habit (late completion, skipping, backfill, etc.).
/// </summary>
public sealed class HabitPolicy : ValueObject
{
    /// <summary>
    /// Whether late completion is allowed (completing after the day ends).
    /// </summary>
    public bool AllowLateCompletion { get; }

    /// <summary>
    /// Cutoff time for late completion (e.g., 03:00 next day).
    /// Only used when AllowLateCompletion is true.
    /// </summary>
    public TimeOnly? LateCutoffTime { get; }

    /// <summary>
    /// Whether the user can explicitly skip occurrences.
    /// </summary>
    public bool AllowSkip { get; }

    /// <summary>
    /// Whether a reason is required when marking as missed.
    /// </summary>
    public bool RequireMissReason { get; }

    /// <summary>
    /// Whether the user can backfill past occurrences.
    /// </summary>
    public bool AllowBackfill { get; }

    /// <summary>
    /// Maximum number of days in the past that can be backfilled.
    /// </summary>
    public int MaxBackfillDays { get; }

    // Required for EF Core and JSON deserialization
    private HabitPolicy()
    {
        AllowLateCompletion = true;
        LateCutoffTime = new TimeOnly(3, 0); // 3 AM
        AllowSkip = true;
        RequireMissReason = false;
        AllowBackfill = true;
        MaxBackfillDays = 7;
    }

    [JsonConstructor]
    public HabitPolicy(
        bool allowLateCompletion,
        TimeOnly? lateCutoffTime,
        bool allowSkip,
        bool requireMissReason,
        bool allowBackfill,
        int maxBackfillDays)
    {
        AllowLateCompletion = allowLateCompletion;
        LateCutoffTime = lateCutoffTime;
        AllowSkip = allowSkip;
        RequireMissReason = requireMissReason;
        AllowBackfill = allowBackfill;
        MaxBackfillDays = maxBackfillDays;
    }

    /// <summary>
    /// Creates a default habit policy with sensible defaults.
    /// </summary>
    public static HabitPolicy Default()
    {
        return new HabitPolicy(
            allowLateCompletion: true,
            lateCutoffTime: new TimeOnly(3, 0), // 3 AM
            allowSkip: true,
            requireMissReason: false,
            allowBackfill: true,
            maxBackfillDays: 7);
    }

    /// <summary>
    /// Creates a strict policy (no late completion, no skipping, requires miss reason).
    /// </summary>
    public static HabitPolicy Strict()
    {
        return new HabitPolicy(
            allowLateCompletion: false,
            lateCutoffTime: null,
            allowSkip: false,
            requireMissReason: true,
            allowBackfill: false,
            maxBackfillDays: 0);
    }

    /// <summary>
    /// Creates a flexible policy (generous late completion, allows everything).
    /// </summary>
    public static HabitPolicy Flexible()
    {
        return new HabitPolicy(
            allowLateCompletion: true,
            lateCutoffTime: new TimeOnly(6, 0), // 6 AM
            allowSkip: true,
            requireMissReason: false,
            allowBackfill: true,
            maxBackfillDays: 14);
    }

    /// <summary>
    /// Creates a policy with validation.
    /// </summary>
    public static HabitPolicy Create(
        bool allowLateCompletion = true,
        TimeOnly? lateCutoffTime = null,
        bool allowSkip = true,
        bool requireMissReason = false,
        bool allowBackfill = true,
        int maxBackfillDays = 7)
    {
        if (maxBackfillDays < 0)
            throw new DomainException("Max backfill days cannot be negative.");

        if (maxBackfillDays > 30)
            throw new DomainException("Max backfill days cannot exceed 30.");

        if (allowLateCompletion && !lateCutoffTime.HasValue)
            lateCutoffTime = new TimeOnly(3, 0); // Default to 3 AM

        return new HabitPolicy(
            allowLateCompletion,
            lateCutoffTime,
            allowSkip,
            requireMissReason,
            allowBackfill,
            maxBackfillDays);
    }

    /// <summary>
    /// Returns a new policy with updated late completion settings.
    /// </summary>
    public HabitPolicy WithLateCompletion(bool allow, TimeOnly? cutoff = null)
    {
        return new HabitPolicy(
            allow,
            allow ? (cutoff ?? LateCutoffTime ?? new TimeOnly(3, 0)) : null,
            AllowSkip,
            RequireMissReason,
            AllowBackfill,
            MaxBackfillDays);
    }

    /// <summary>
    /// Returns a new policy with updated backfill settings.
    /// </summary>
    public HabitPolicy WithBackfill(bool allow, int maxDays = 7)
    {
        if (maxDays < 0 || maxDays > 30)
            throw new DomainException("Max backfill days must be between 0 and 30.");

        return new HabitPolicy(
            AllowLateCompletion,
            LateCutoffTime,
            AllowSkip,
            RequireMissReason,
            allow,
            allow ? maxDays : 0);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AllowLateCompletion;
        yield return LateCutoffTime;
        yield return AllowSkip;
        yield return RequireMissReason;
        yield return AllowBackfill;
        yield return MaxBackfillDays;
    }

    public override string ToString()
    {
        var parts = new List<string>();

        if (AllowLateCompletion)
            parts.Add($"Late OK (until {LateCutoffTime})");
        if (AllowSkip)
            parts.Add("Skip OK");
        if (AllowBackfill)
            parts.Add($"Backfill {MaxBackfillDays}d");
        if (RequireMissReason)
            parts.Add("Reason Required");

        return parts.Count > 0 ? string.Join(", ", parts) : "Strict";
    }
}
